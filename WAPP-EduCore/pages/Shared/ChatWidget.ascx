<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ChatWidget.ascx.cs" Inherits="WAPP_EduCore.pages.Shared.ChatWidget" %>
<style>
  /* ---------- floating 3D orb button ---------- */
  #chatFab { position:fixed; right:24px; bottom:24px; width:64px; height:64px; padding:0; border:0;
             background:none; cursor:pointer; z-index:1000; perspective:260px;
             animation:botFloat 3s ease-in-out infinite; transition:transform .2s; }
  #chatFab:hover { transform:scale(1.1); }
  #chatFab::before { content:''; position:absolute; inset:-6px; border-radius:50%;
             background:radial-gradient(circle, rgba(59,130,246,.45), transparent 70%);
             animation:botGlow 2.4s ease-in-out infinite; }

  .bot-orb { position:absolute; inset:8px; border-radius:50%;
             background:radial-gradient(circle at 30% 26%, #bfdbfe 0%, #3b82f6 38%, #1d4ed8 72%, #1e3a8a 100%);
             box-shadow:inset -7px -9px 16px rgba(15,23,42,.45),
                        inset 5px 5px 10px rgba(255,255,255,.35),
                        0 8px 20px rgba(37,99,235,.45); }
  .bot-ring { position:absolute; inset:0; border-radius:50%; border:2px solid rgba(147,197,253,.75);
              pointer-events:none; }
  .bot-ring.r1 { animation:ringA 4s linear infinite; }
  .bot-ring.r2 { border-color:rgba(196,181,253,.7); animation:ringB 6s linear infinite; }

  .bot-eyes { position:absolute; left:0; right:0; top:38%; display:flex; justify-content:center;
              gap:9px; transition:transform .12s ease-out; }
  .bot-eyes span { width:8px; height:11px; border-radius:50%; background:#fff;
                   box-shadow:0 0 6px rgba(255,255,255,.9); animation:botBlink 4.5s infinite; }

  @keyframes botFloat { 0%,100% { transform:translateY(0); } 50% { transform:translateY(-6px); } }
  @keyframes botGlow  { 0%,100% { opacity:.55; transform:scale(1); } 50% { opacity:1; transform:scale(1.12); } }
  @keyframes ringA { from { transform:rotateX(72deg) rotateZ(0deg); } to { transform:rotateX(72deg) rotateZ(360deg); } }
  @keyframes ringB { from { transform:rotateY(72deg) rotateZ(0deg); } to { transform:rotateY(72deg) rotateZ(-360deg); } }
  @keyframes botBlink { 0%,92%,100% { transform:scaleY(1); } 96% { transform:scaleY(.1); } }

  @media (prefers-reduced-motion: reduce) {
    #chatFab, #chatFab::before, .bot-ring, .bot-eyes span { animation:none; }
  }

  /* ---------- chat window ---------- */
  #chatBox { position:fixed; right:24px; bottom:100px; width:340px; max-width:calc(100vw - 32px); height:440px;
             background:#fff; border:1px solid #e2e8f0; border-radius:12px; display:none;
             flex-direction:column; box-shadow:0 8px 30px rgba(0,0,0,.15); z-index:1000;
             font-family:'Inter',sans-serif; }
  #chatHead { padding:12px 14px; font-weight:600; border-bottom:1px solid #e2e8f0; color:#0f172a; }
  #chatMsgs { flex:1; overflow-y:auto; padding:12px; }
  .cm { margin:6px 0; padding:8px 10px; border-radius:10px; max-width:85%; white-space:pre-wrap;
        font-size:14px; line-height:1.4; }
  .cm.user { background:#2563eb; color:#fff; margin-left:auto; }
  .cm.bot  { background:#f1f5f9; color:#0f172a; }
  #chatForm { display:flex; border-top:1px solid #e2e8f0; }
  #chatInput { flex:1; border:0; padding:12px; outline:none; font-size:14px; background:transparent; }
  #chatSend { border:0; background:none; color:#2563eb; font-weight:600; padding:0 14px; cursor:pointer; }
  #chatSend:disabled { color:#94a3b8; cursor:default; }
</style>

<button type="button" id="chatFab" title="Ask the assistant" aria-label="Open chat assistant">
  <span class="bot-ring r1"></span>
  <span class="bot-ring r2"></span>
  <span class="bot-orb">
    <span class="bot-eyes" id="botEyes"><span></span><span></span></span>
  </span>
</button>

<div id="chatBox" role="dialog" aria-label="EduCore assistant">
  <div id="chatHead">EduCore Assistant</div>
  <div id="chatMsgs">
    <div class="cm bot">Hi! Ask me about courses or study tips.</div>
  </div>
  <div id="chatForm">
    <input type="text" id="chatInput" maxlength="500" placeholder="Type a message..." autocomplete="off" />
    <button type="button" id="chatSend">Send</button>
  </div>
</div>

<script>
// @ts-nocheck
(function () {
    var url = '<%= ResolveUrl("~/pages/Shared/ChatHandler.ashx") %>';
  var fab   = document.getElementById('chatFab');
  var box   = document.getElementById('chatBox');
  var msgs  = document.getElementById('chatMsgs');
  var input = document.getElementById('chatInput');
  var send  = document.getElementById('chatSend');
  var eyes  = document.getElementById('botEyes');
  var history = [];
  var busy = false;

  // open / close the chat window
  fab.onclick = function () {
    box.style.display = (box.style.display === 'flex') ? 'none' : 'flex';
    if (box.style.display === 'flex') input.focus();
  };

  // eyes follow the mouse
  document.addEventListener('mousemove', function (e) {
    var r  = fab.getBoundingClientRect();
    var dx = (e.clientX - (r.left + r.width / 2)) / window.innerWidth;
    var dy = (e.clientY - (r.top + r.height / 2)) / window.innerHeight;
    eyes.style.transform = 'translate(' + (dx * 10) + 'px,' + (dy * 7) + 'px)';
  });

  function add(text, who) {
    var d = document.createElement('div');
    d.className = 'cm ' + who;
    d.textContent = text;              // textContent (not innerHTML) to prevent XSS
    msgs.appendChild(d);
    msgs.scrollTop = msgs.scrollHeight;
    return d;
  }

  function submit() {
    var text = input.value.trim();
    if (!text || busy) return;

    busy = true;
    send.disabled = true;
    input.value = '';
    add(text, 'user');
    var thinking = add('...', 'bot');

    var body = 'message=' + encodeURIComponent(text) +
               '&history=' + encodeURIComponent(JSON.stringify(history.slice(-6)));

    fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: body,
      credentials: 'same-origin'
    })
    .then(function (r) { return r.json().then(function (j) { return { ok: r.ok, reply: j.reply }; }); })
    .then(function (res) {
      thinking.textContent = res.reply || 'No response.';
      if (res.ok) {
        history.push({ role: 'user', text: text });
        history.push({ role: 'model', text: res.reply });
      }
    })
    .catch(function () { thinking.textContent = 'Connection problem. Please try again.'; })
    .then(function () {
      busy = false;
      send.disabled = false;
      input.focus();
      msgs.scrollTop = msgs.scrollHeight;
    });
  }

  send.onclick = submit;
  input.onkeydown = function (e) {
    if (e.key === 'Enter') { e.preventDefault(); submit(); }
  };
})();
</script>