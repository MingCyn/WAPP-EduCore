<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ChatWidget.ascx.cs" Inherits="WAPP_EduCore.pages.Shared.ChatWidget" %>
<link href="<%= ResolveUrl("~/Content/chatbot.css") %>" rel="stylesheet" type="text/css" />

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
    // move the widget out of the sidebar so no parent can trap or clip it
    ['chatFab', 'chatBox'].forEach(function (id) {
        var el = document.getElementById(id);
        if (el && el.parentNode !== document.body) document.body.appendChild(el);
    });

    var url = '<%= ResolveUrl("~/pages/Shared/ChatHandler.ashx") %>';
    var fab = document.getElementById('chatFab');
    var box = document.getElementById('chatBox');
    var msgs = document.getElementById('chatMsgs');
    var input = document.getElementById('chatInput');
    var send = document.getElementById('chatSend');
    var eyes = document.getElementById('botEyes');
    var history = [];
    var busy = false;

    // open / close the chat window
    fab.onclick = function () {
        box.style.display = (box.style.display === 'flex') ? 'none' : 'flex';
        if (box.style.display === 'flex') input.focus();
    };

    // eyes follow the mouse (Fixed syntax)
    document.addEventListener('mousemove', function (e) {
        var r = fab.getBoundingClientRect();
        var centerX = r.left + r.width / 2;
        var centerY = r.top + r.height / 2;
        var dx = (e.clientX - centerX) / window.innerWidth;
        var dy = (e.clientY - centerY) / window.innerHeight;
        eyes.style.transform = 'translate(' + (dx * 10) + 'px,' + (dy * 7) + 'px)';
    });

    function add(text, who) {
        var d = document.createElement('div');
        d.className = 'cm ' + who;
        d.textContent = text; // textContent (not innerHTML) to prevent XSS
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