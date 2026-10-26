/*
 * "Sign in with Google" and cloud saves for Pooket Slug, without the Firebase SDK
 * (docs/research/accounts/README.md). Ported from Cardmaster's src/net/auth.ts, google.ts and rtdb.ts.
 *
 *   Google's button ──► Google ID token ──► Firebase Auth (REST): uid, ID token, refresh token
 *   ──► the Realtime Database (REST): pooketSlug/players/<uid>, under the rules in Cardmaster's repository
 *   ──► the game, through PooketAuth.jslib and SendMessage
 *
 * The account area (#account) sits over the top left of the game; the game shows it on its title screen and
 * settings only (PooketAuth.show).
 *
 * The settings come from pooket-config.js, which the build writes from the repository variables. Without
 * them the account area stays empty and the game keeps its progress in this browser only.
 *
 * The game and this script talk in the game's own profile shape (Profile.cs, as JsonUtility writes it).
 * This script turns it into the database's record and back, and keeps the record's save number (`rev`).
 *
 * Storage keys start with "pooket-slug." because Pooket Tabks shares this origin and its localStorage.
 */
(function () {
  "use strict";

  var AUTH_KEY = "pooket-slug.auth";
  var RECEIVER = "PooketAuth"; // the game object the game creates for SendMessage (CloudAccount.cs)
  var REFRESH_EARLY_MS = 60000; // refresh the ID token this long before it lapses
  var FRESH_MS = 30000; // a forced refresh is skipped while the token is younger than this
  var DEAD = /TOKEN_EXPIRED|INVALID_REFRESH_TOKEN|USER_DISABLED|USER_NOT_FOUND|INVALID_GRANT_TYPE|MISSING_REFRESH_TOKEN|PROJECT_NUMBER_MISMATCH/;
  var SERVER_TIME = { ".sv": "timestamp" };
  var SETTINGS = ["normal", "hard"]; // the game's Setting enum, by number
  var ID_MAX = 32, NAME_MAX = 24;

  var config = window.POOKET_CONFIG || {};
  var enabled = !!(config.googleClientId && config.firebaseApiKey && config.firebaseDbUrl);

  // ------------------------------------------------------------------ the session (Firebase Auth over REST)

  var session = loadSession(); // { uid, idToken, refreshToken, expiresAt, name }
  var refreshing = null;
  var freshAt = 0;

  function loadSession() {
    try {
      var s = JSON.parse(localStorage.getItem(AUTH_KEY) || "null");
      return s && typeof s.uid === "string" && typeof s.idToken === "string" &&
        typeof s.refreshToken === "string" && typeof s.expiresAt === "number" ? s : null;
    } catch (e) {
      return null;
    }
  }

  function setSession(s) {
    var changed = (s && s.uid) !== (session && session.uid);
    session = s;
    try {
      if (s) localStorage.setItem(AUTH_KEY, JSON.stringify(s));
      else localStorage.removeItem(AUTH_KEY);
    } catch (e) {
      // private browsing and the like: signed in until the page closes
    }
    return changed;
  }

  function post(api, contentType, body) {
    var base = config.authEmulator ? config.authEmulator + "/" + api : "https://" + api;
    return fetch(base + "?key=" + encodeURIComponent(config.firebaseApiKey), {
      method: "POST",
      headers: { "Content-Type": contentType },
      body: body,
      signal: timeout(10000),
    });
  }

  function timeout(ms) {
    return typeof AbortSignal !== "undefined" && AbortSignal.timeout ? AbortSignal.timeout(ms) : undefined;
  }

  function expiry(seconds) {
    return Date.now() + (Number(seconds) || 3600) * 1000;
  }

  /** Swaps Google's ID token for a Firebase account. */
  async function signInWithGoogle(googleIdToken) {
    var res;
    try {
      res = await post("identitytoolkit.googleapis.com/v1/accounts:signInWithIdp", "application/json", JSON.stringify({
        postBody: "id_token=" + encodeURIComponent(googleIdToken) + "&providerId=google.com",
        requestUri: location.origin,
        returnSecureToken: true,
      }));
    } catch (e) {
      throw new Error("Couldn't reach the sign-in service");
    }
    var body = await res.json().catch(function () { return null; });
    if (!res.ok || !body || !body.localId || !body.idToken || !body.refreshToken) {
      throw new Error("Sign-in was refused" + (body && body.error && body.error.message ? " (" + body.error.message + ")" : ""));
    }
    freshAt = Date.now();
    var first = body.firstName || (body.displayName || "").split(/\s+/)[0] || "";
    setSession({ uid: body.localId, idToken: body.idToken, refreshToken: body.refreshToken, expiresAt: expiry(body.expiresIn), name: first });
  }

  /**
   * A token the database accepts, or null when signed out. `force` refreshes it (the database just refused
   * it) unless it's brand new. Offline isn't signed out: the current token comes back while it lasts.
   */
  function token(force) {
    var s = session;
    if (!s) return Promise.resolve(null);
    var lasting = s.expiresAt - Date.now() > REFRESH_EARLY_MS;
    if (lasting && !(force && Date.now() - freshAt > FRESH_MS)) return Promise.resolve(s.idToken);
    if (!refreshing) refreshing = refresh(s).finally(function () { refreshing = null; });
    return refreshing;
  }

  async function refresh(s) {
    try {
      var res = await post("securetoken.googleapis.com/v1/token", "application/x-www-form-urlencoded",
        "grant_type=refresh_token&refresh_token=" + encodeURIComponent(s.refreshToken));
      var body = await res.json().catch(function () { return null; });
      if (!session || session.uid !== s.uid) return null; // signed out, or in as someone else, meanwhile
      if (res.ok && body && body.id_token && body.refresh_token) {
        freshAt = Date.now();
        setSession({ uid: s.uid, idToken: body.id_token, refreshToken: body.refresh_token, expiresAt: expiry(body.expires_in), name: s.name });
        return body.id_token;
      }
      if (res.status >= 400 && res.status < 500 && DEAD.test((body && body.error && body.error.message) || "")) {
        signOut();
        return null;
      }
    } catch (e) {
      // no connection: fall through
    }
    return session && session.expiresAt > Date.now() ? session.idToken : null;
  }

  // ------------------------------------------------------------------ the database (Realtime Database over REST)

  /** One call; on a 401 the token is refreshed and the call tried once more. Resolves to { status, body }. */
  async function db(method, path, value) {
    var t = await token(false);
    var res = await send(method, path, t, value);
    if (res.status === 401 && t) {
      t = await token(true);
      if (t) res = await send(method, path, t, value);
    }
    var text = await res.text().catch(function () { return ""; });
    var body = null;
    try { body = text ? JSON.parse(text) : null; } catch (e) { body = text; }
    return { status: res.status, ok: res.ok, body: body };
  }

  async function send(method, path, t, value) {
    var url = new URL(config.firebaseDbUrl.replace(/\/+$/, "") + "/" + path + ".json");
    if (config.dbNamespace) url.searchParams.set("ns", config.dbNamespace);
    if (t) url.searchParams.set("auth", t);
    try {
      return await fetch(url, { method: method, body: value === undefined ? undefined : JSON.stringify(value), signal: timeout(10000) });
    } catch (e) {
      return new Response("", { status: 599, statusText: "no connection" }); // 599: couldn't reach the database
    }
  }

  function recordPath() {
    return "pooketSlug/players/" + session.uid;
  }

  // ------------------------------------------------------------------ the record and the game's profile

  /** The database record as the game's Profile (JsonUtility's shape: lists, and the setting as a number). */
  function toProfile(rec) {
    rec = rec && typeof rec === "object" ? rec : {};
    var missions = [];
    var m = rec.missions || {};
    Object.keys(m).forEach(function (mission) {
      SETTINGS.forEach(function (name, setting) {
        var r = m[mission] && m[mission][name];
        if (r) missions.push({ mission: mission, setting: setting, best: r.best | 0, clears: r.clears | 0 });
      });
    });
    return {
      version: rec.version || 1,
      coop: rec.coop === true,
      xp: typeof rec.xp === "number" ? Math.max(0, rec.xp | 0) : 0,
      rank: typeof rec.rank === "number" ? Math.max(0, rec.rank | 0) : 0,
      missions: missions,
      seen: Object.keys(rec.seen || {}),
      first: typeof rec.first === "number" ? rec.first : 0,
      ts: typeof rec.ts === "number" ? rec.ts : 0,
    };
  }

  /** The fields of the record the game owns, from its profile. Lists the rules would refuse are left out. */
  function fromProfile(p) {
    var fields = { version: Math.max(1, Math.min(1000, p.version | 0)), coop: p.coop === true,
      xp: Math.min(1000000000, Math.max(0, p.xp | 0)), rank: Math.min(1000, Math.max(0, p.rank | 0)) };
    var missions = {};
    (p.missions || []).forEach(function (r) {
      var setting = SETTINGS[r.setting];
      if (!setting || !r.mission || r.mission.length > ID_MAX || /[.#$\[\]\/]/.test(r.mission)) return;
      (missions[r.mission] = missions[r.mission] || {})[setting] = { best: Math.max(0, r.best | 0), clears: Math.max(0, r.clears | 0) };
    });
    if (Object.keys(missions).length) fields.missions = missions;
    var seen = {};
    (p.seen || []).forEach(function (id) {
      if (id && id.length <= ID_MAX && !/[.#$\[\]\/]/.test(id)) seen[id] = true;
    });
    if (Object.keys(seen).length) fields.seen = seen;
    return fields;
  }

  function playerName() {
    var n = ((session && session.name) || "").trim().slice(0, NAME_MAX);
    return n.length ? n : null;
  }

  // ------------------------------------------------------------------ cloud saves

  // rev: the record's save number as last read or written; null until it's been read this sign-in.
  var cloud = { uid: null, rev: null, hasName: false, loading: null };
  var saving = Promise.resolve();
  var pendingSave = null;

  /** Reads the player's record and hands it to the game, which merges it and saves the result. */
  function loadCloud() {
    if (!session) return Promise.resolve();
    var uid = session.uid;
    cloud = { uid: uid, rev: null, hasName: false, loading: null };
    cloud.loading = (async function () {
      setStatus("Loading your progress…");
      var r = await db("GET", recordPath());
      if (!session || session.uid !== uid) return;
      if (!r.ok) {
        setStatus(r.status === 599 ? "Offline: progress is kept on this device" : "Couldn't load your progress");
        log("load", r.status, r.body);
        cloud.loading = null;
        return;
      }
      var rec = r.body && typeof r.body === "object" ? r.body : null;
      cloud.rev = rec && typeof rec.rev === "number" ? rec.rev : 0;
      cloud.hasName = !!(rec && rec.name);
      cloud.loading = null;
      setStatus("");
      toGame("OnCloudProfile", JSON.stringify(toProfile(rec)));
    })();
    return cloud.loading;
  }

  /** The game's save: written once the record has been read, one save at a time, the newest winning. */
  function save(profileJson) {
    if (!session || cloud.uid !== session.uid || cloud.rev === null) return; // the merge after loading saves again
    pendingSave = profileJson;
    saving = saving.then(function () {
      var json = pendingSave;
      pendingSave = null;
      return json ? write(json, true) : null;
    }).catch(function (e) { log("save", e); });
  }

  async function write(profileJson, mayRetry) {
    if (!session || cloud.uid !== session.uid || cloud.rev === null) return;
    var uid = session.uid;
    var profile;
    try { profile = JSON.parse(profileJson); } catch (e) { return; }
    var fields = fromProfile(profile);
    var rev = cloud.rev + 1;
    var r;
    fields.rev = rev;
    fields.ts = SERVER_TIME;
    var name = playerName();
    if (cloud.rev === 0) {
      // The first save makes the whole record.
      fields.tokens = 0;
      fields.first = SERVER_TIME;
      if (name) fields.name = name;
      r = await db("PUT", recordPath(), fields);
    } else {
      if (!cloud.hasName && name) fields.name = name;
      r = await db("PATCH", recordPath(), fields);
    }
    if (!session || session.uid !== uid) return;
    if (r.ok) {
      cloud.rev = rev;
      if (fields.name) cloud.hasName = true;
      setStatus("");
      return;
    }
    log("save", r.status, r.body);
    if (r.status === 599) {
      setStatus("Offline: progress is kept on this device");
      return;
    }
    // Refused: another device has probably saved since. Read the record again; if its save number moved,
    // the game merges it and saves once more. If it didn't, the rules refused the record itself: stop.
    var seen = cloud.rev;
    var again = await db("GET", recordPath());
    if (!session || session.uid !== uid || !again.ok) return;
    var rec = again.body && typeof again.body === "object" ? again.body : null;
    var now = rec && typeof rec.rev === "number" ? rec.rev : 0;
    if (now !== seen && mayRetry) {
      cloud.rev = now;
      cloud.hasName = !!(rec && rec.name);
      toGame("OnCloudProfile", JSON.stringify(toProfile(rec)));
    } else {
      setStatus("Couldn't save your progress online");
    }
  }

  // ------------------------------------------------------------------ the game (PooketAuth.jslib calls in here)

  var unity = null; // the unityInstance, once the page has it
  var gameReady = false; // the game has called PS_AuthInit, so its receiver exists
  var queue = []; // messages for the game until both are true

  function toGame(method, arg) {
    queue.push([method, arg]);
    flush();
  }

  function flush() {
    if (!unity || !gameReady) return;
    var q = queue;
    queue = [];
    q.forEach(function (m) {
      try { unity.SendMessage(RECEIVER, m[0], m[1]); } catch (e) { log("SendMessage", m[0], e); }
    });
  }

  function accountToGame() {
    toGame("OnAccount", JSON.stringify({ available: enabled, signedIn: !!session, name: playerName() || "" }));
  }

  // ------------------------------------------------------------------ the page's account area

  var area = null, google = null;

  function setStatus(text) {
    var el = area && area.querySelector(".account-status");
    if (el) el.textContent = text || "";
  }

  function render() {
    if (!area) return;
    area.hidden = !enabled;
    if (!enabled) return;
    area.replaceChildren();
    if (session) {
      var who = document.createElement("span");
      who.className = "account-name";
      who.textContent = "Signed in" + (playerName() ? " as " + playerName() : "");
      var out = document.createElement("button");
      out.type = "button";
      out.textContent = "Sign out";
      out.addEventListener("click", function () { signOut(); refocus(); });
      area.append(who, out);
    } else {
      var slot = document.createElement("span");
      slot.className = "account-button";
      area.append(slot);
      loadGoogle().then(function (id) {
        if (session || !slot.isConnected) return;
        id.renderButton(slot, { type: "standard", theme: "filled_black", size: "small", text: "signin", shape: "pill" });
      }).catch(function (e) {
        setStatus(e.message);
      });
    }
    var status = document.createElement("span");
    status.className = "account-status";
    area.append(status);
  }

  function refocus() {
    var canvas = document.querySelector("#unity-canvas");
    if (canvas) canvas.focus();
  }

  function loadGoogle() {
    if (google) return google;
    var fake = new URLSearchParams(location.search).get("fakegoogle");
    if (fake && config.authEmulator) {
      google = Promise.resolve(fakeGoogle(fake)).then(initGoogle);
      return google;
    }
    google = new Promise(function (resolve, reject) {
      var s = document.createElement("script");
      s.src = "https://accounts.google.com/gsi/client";
      s.async = true;
      s.onload = function () {
        var id = window.google && window.google.accounts && window.google.accounts.id;
        if (id) resolve(id);
        else reject(new Error("Google's sign-in didn't start"));
      };
      s.onerror = function () {
        s.remove();
        reject(new Error("Couldn't reach Google"));
      };
      document.head.append(s);
    }).then(initGoogle).catch(function (e) {
      google = null;
      throw e;
    });
    return google;
  }

  function initGoogle(id) {
    id.initialize({
      client_id: config.googleClientId,
      callback: onGoogleCredential,
      context: "signin",
      use_fedcm_for_button: true,
    });
    return id;
  }

  /** For tests against the Auth emulator (`?fakegoogle=Ann`): a button that signs in as Ann at once. */
  function fakeGoogle(name) {
    var callback = function () {};
    return {
      initialize: function (c) { callback = c.callback; },
      renderButton: function (parent) {
        var b = document.createElement("button");
        b.type = "button";
        b.className = "fake-google";
        b.textContent = "Sign in as " + name;
        b.addEventListener("click", function () {
          var sub = "fake-" + name.toLowerCase().replace(/\W+/g, "-");
          callback({ credential: JSON.stringify({ sub: sub, email: sub + "@example.com", email_verified: true, name: name, given_name: name.split(/\s+/)[0] }) });
        });
        parent.replaceChildren(b);
      },
      disableAutoSelect: function () {},
    };
  }

  async function onGoogleCredential(response) {
    if (!response || !response.credential) return;
    setStatus("Signing in…");
    try {
      await signInWithGoogle(response.credential);
    } catch (e) {
      setStatus(e.message);
      return;
    }
    changed();
    refocus();
  }

  function signOut() {
    if (!session) return;
    setSession(null);
    try {
      var id = window.google && window.google.accounts && window.google.accounts.id;
      if (id) id.disableAutoSelect();
    } catch (e) { /* not loaded */ }
    changed();
  }

  /** Signed in or out, here or in another tab. */
  function changed() {
    cloud = { uid: null, rev: null, hasName: false, loading: null };
    render();
    accountToGame();
    if (session) loadCloud();
  }

  function log() {
    var args = Array.prototype.slice.call(arguments);
    args.unshift("[PooketAuth]");
    console.warn.apply(console, args);
  }

  // ------------------------------------------------------------------ start

  function start() {
    area = document.querySelector("#account");
    if (!enabled) {
      if (area) area.hidden = true;
      return;
    }
    render();
    if (session) loadCloud();
    window.addEventListener("storage", function (e) {
      if (e.key !== AUTH_KEY) return;
      var s = loadSession();
      var before = session && session.uid;
      session = s;
      if ((s && s.uid) !== before) changed();
    });
  }

  window.PooketAuth = {
    /** The page hands over the unityInstance once the game has loaded. */
    attach: function (instance) {
      unity = instance;
      flush();
    },
    // From PooketAuth.jslib:
    gameReady: function () {
      gameReady = true;
      // The game's first word is the account state; a profile loaded before it is still queued behind it.
      queue.unshift(["OnAccount", JSON.stringify({ available: enabled, signedIn: !!session, name: playerName() || "" })]);
      flush();
    },
    save: function (profileJson) {
      if (enabled) save(profileJson);
    },
    signOut: signOut,
    /** The game shows its sign-in button on the title screen and settings, and hides it while playing. */
    show: function (on) {
      if (area) area.style.visibility = on ? "" : "hidden"; // kept in place, so Google's button needn't be drawn again
    },
    // For tests:
    _state: function () { return { enabled: enabled, uid: session && session.uid, rev: cloud.rev, queued: queue.length }; },
    _toProfile: toProfile,
    _fromProfile: fromProfile,
  };

  if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", start);
  else start();
})();
