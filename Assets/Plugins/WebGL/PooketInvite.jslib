// The den's invite button (rooms): shares the room's link on a phone (the share sheet) or copies it elsewhere.
// Browsers only share or copy inside a tap's own event, and the game hears about a tap a frame later, so the game
// arms the invite as the finger goes down (PS_InviteArm) and the page shares it as the finger comes up. The game
// reads how it went with PS_InviteResult: 0 nothing yet, 1 shared, 2 copied, 3 couldn't (the game then shows the link).
// A tap quicker than a frame has lifted before it's armed: then it tries anyway a moment later, which works wherever
// the browser lets a tap count for a few seconds (Chrome); elsewhere it reports 3.
mergeInto(LibraryManager.library, {
  PS_InviteArm: function (url, text) {
    var link = UTF8ToString(url), words = UTF8ToString(text);
    window.pooketInviteResult = 0;
    if (window.pooketInviteDisarm) window.pooketInviteDisarm();

    function done(r) { window.pooketInviteResult = r; }
    function copy() {
      if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(link).then(function () { done(2); }, copyOld);
      } else copyOld();
    }
    function copyOld() {
      try {
        var box = document.createElement("textarea");
        box.value = link;
        box.setAttribute("readonly", "");
        box.style.position = "fixed";
        box.style.opacity = "0";
        document.body.appendChild(box);
        box.select();
        var ok = document.execCommand("copy");
        document.body.removeChild(box);
        done(ok ? 2 : 3);
      } catch (e) { done(3); }
    }
    function go() {
      disarm();
      var phone = window.matchMedia && window.matchMedia("(pointer: coarse)").matches;
      if (phone && navigator.share) {
        navigator.share({ title: "Pooket Slug", text: words, url: link }).then(function () { done(1); }, function (e) {
          done(e && e.name === "AbortError" ? 0 : 3); // closing the share sheet isn't a failure
        });
      } else copy();
    }
    function disarm() {
      clearTimeout(late);
      window.removeEventListener("pointerup", go, true);
      window.removeEventListener("touchend", go, true);
      window.removeEventListener("keyup", go, true);
      window.pooketInviteDisarm = null;
    }
    var late = setTimeout(go, 400);
    window.addEventListener("pointerup", go, true);
    window.addEventListener("touchend", go, true);
    window.addEventListener("keyup", go, true);
    window.pooketInviteDisarm = disarm;
  },

  PS_InviteResult: function () {
    var r = window.pooketInviteResult || 0;
    window.pooketInviteResult = 0;
    return r;
  },
});
