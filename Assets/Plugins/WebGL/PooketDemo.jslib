// The kit screen's demo recorder (tools/demos.mjs): while ?demo=all plays each move's demo, the game records the canvas
// itself, from the frame a demo starts to the frame it ends, so each clip holds exactly its demo. The clips wait in
// window.pooketDemoClips[id] = { blob, seconds } (seconds of wall clock it took) for the recorder to collect.
mergeInto(LibraryManager.library, {
  PS_DemoBegin: function (id) {
    var name = UTF8ToString(id);
    var canvas = Module.canvas || document.querySelector("#unity-canvas");
    if (!canvas || !canvas.captureStream || typeof MediaRecorder === "undefined") return;
    var clips = (window.pooketDemoClips = window.pooketDemoClips || {});
    var chunks = [];
    var recorder = new MediaRecorder(canvas.captureStream(30), { mimeType: "video/webm;codecs=vp8", videoBitsPerSecond: 6000000 });
    var started = performance.now();
    recorder.ondataavailable = function (e) { if (e.data && e.data.size) chunks.push(e.data); };
    recorder.onstop = function () {
      clips[name] = { blob: new Blob(chunks, { type: "video/webm" }), seconds: (performance.now() - started) / 1000 };
    };
    recorder.start();
    window.pooketDemoRecorder = recorder;
  },

  PS_DemoEnd: function () {
    var recorder = window.pooketDemoRecorder;
    window.pooketDemoRecorder = null;
    if (recorder && recorder.state !== "inactive") recorder.stop();
  },
});
