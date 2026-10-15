<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="quiz.aspx.cs" Inherits="WAPP_EduCore.pages.General.quiz" %>
<%@ Register Src="~/pages/Shared/navDashboard.ascx" TagPrefix="uc" TagName="NavDashboard" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Quiz | EduCore</title>
    <link rel="stylesheet" href="/Content/navDashboard.css" />
    <link rel="stylesheet" href="/Content/site.css" />
    <link rel="stylesheet" href="/Content/course.css" />

        <style>
        /* ---------- Result modal ---------- */

        .overlay { position: fixed; inset: 0; z-index: 50; display: grid; place-items: center; padding: 16px;

                   background: rgba(15, 26, 61, .55); backdrop-filter: blur(3px); }

        .modal { position: relative; z-index: 2; width: 100%; max-width: 400px; padding: 34px 28px 28px; text-align: center;

                 border-radius: 18px; background: linear-gradient(160deg, #fbe9ff 0%, #eef0ff 50%, #d9f7ff 100%);

                 box-shadow: 0 20px 60px rgba(0,0,0,.3); animation: pop .35s ease-out; }

        @keyframes pop { from { transform: scale(.9); opacity: 0; } to { transform: scale(1); opacity: 1; } }

        .gift { width: 84px; height: 84px; margin: -72px auto 14px; border-radius: 20px; display: grid; place-items: center;

                background: #2a1470; font-size: 40px; box-shadow: 0 0 0 3px #6aa7ff, 0 0 24px #9d6bff; }

        .modal h2 { margin: 0 0 6px; font: 800 28px "Segoe UI", sans-serif; color: #14103a; }

        .grade { font: 800 64px/1 Georgia, serif; color: var(--navy); margin: 10px 0 4px; }

        .score { font-size: 16px; font-weight: 600; margin-bottom: 6px; }

        .verdict { display: inline-block; padding: 4px 14px; border-radius: 99px; font-size: 13px; font-weight: 700; margin-bottom: 14px; }

        .verdict.pass { background: #c8f0cc; color: #1b5e20; }

        .verdict.fail { background: #ffd9d6; color: #8e1b14; }

        .note { font-size: 13px; color: var(--muted); margin: 0 0 20px; line-height: 1.5; }

        .actions { display: flex; gap: 10px; justify-content: center; flex-wrap: wrap; }

        .btn.green { background: #2e9e44; border-color: #2e9e44; color: #fff; }





        /* ---------- Answer review ---------- */

        .rv { max-width: 760px; margin-bottom: 18px; padding: 22px 24px; background: var(--card); border: 1px solid var(--line); border-radius: 10px; }

        .rv-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px; }

        .rv-q { font-size: 12px; font-weight: 600; color: var(--navy); }

        .rv-tag { padding: 3px 10px; border-radius: 99px; font-size: 12px; font-weight: 700; }

        .rv-tag.ok { background: #c8f0cc; color: #1b5e20; }

        .rv-tag.bad { background: #ffd9d6; color: #8e1b14; }

        .rv-tag.skip { background: #ececf2; color: #4a4f68; }

        .rv-text { margin: 0 0 16px; font: 600 18px/1.4 Georgia, "Times New Roman", serif; }

        .rv-opts { list-style: none; margin: 0; padding: 0; }

        .ro { display: flex; align-items: center; gap: 12px; margin-bottom: 8px; padding: 12px 14px; border: 1px solid var(--line);

              border-radius: 6px; background: #faf9f7; font-size: 14px; }

        .ro .key { flex: none; width: 24px; height: 24px; border-radius: 50%; display: grid; place-items: center;

                   background: #fff; border: 1px solid var(--line); font-size: 12px; font-weight: 700; }

        .ro .rt { flex: 1; }

        .ro .rl { flex: none; font-size: 12px; font-weight: 700; }

        .ro.right { background: var(--good-bg); border-color: #7cc27f; }

        .ro.right .key { background: var(--good); border-color: var(--good); color: #fff; }

        .ro.right .rl { color: var(--good); }

        .ro.wrong { background: #fdecea; border-color: #e58a84; }

        .ro.wrong .key { background: var(--bad); border-color: var(--bad); color: #fff; }

        .ro.wrong .rl { color: var(--bad); }

        .expl { margin-top: 14px; padding: 14px 16px; background: var(--good-bg); border: 1px solid #b7dcb9; border-radius: 6px; }

        .expl strong { color: var(--good); font-size: 14px; }

        .expl p { margin: 6px 0 0; font-size: 13px; line-height: 1.55; }

        .topbar .actions { flex-wrap: wrap; }





        /* Grade themes for the result dialog */

        .modal.g-A { background: linear-gradient(160deg, #fff6d6 0%, #ffe9a8 55%, #ffd6f0 100%); }

        .modal.g-A .gift { background: #5a3a00; box-shadow: 0 0 0 3px #ffd54a, 0 0 32px #ffc400; animation: cheer 1.2s ease-in-out 3; }

        .modal.g-A .grade { color: #b57800; }

        .modal.g-B .gift { animation: cheer 1.2s ease-in-out 2; }

        .modal.g-C { background: linear-gradient(160deg, #e6f7ff 0%, #e9f0ff 55%, #f1e9ff 100%); }

        .modal.g-C .gift { background: #12406b; box-shadow: 0 0 0 3px #6ac3ff, 0 0 20px #6ac3ff; animation: pulse 1.8s ease-in-out 2; }

        .modal.g-D { background: linear-gradient(160deg, #fff1e3 0%, #ffe6d6 55%, #f6ecff 100%); }

        .modal.g-D .gift { background: #6b3410; box-shadow: 0 0 0 3px #ffb067, 0 0 16px #ffb067; animation: pulse 2.2s ease-in-out 2; }

        .modal.g-D .grade { color: #b3541b; }

        .modal.g-F { background: linear-gradient(160deg, #eef1f7 0%, #e4e9f4 100%); }

        .modal.g-F .gift { background: #243256; box-shadow: 0 0 0 3px #9aa8d1; animation: pulse 2.6s ease-in-out 2; }

        .modal.g-F .grade { color: #4a5a8a; }

        @keyframes cheer { 0%,100% { transform: translateY(0) rotate(0); } 25% { transform: translateY(-14px) rotate(-8deg); } 60% { transform: translateY(-4px) rotate(8deg); } }

        @keyframes pulse { 0%,100% { transform: scale(1); } 50% { transform: scale(1.08); } }

        @media (prefers-reduced-motion: reduce) { .modal .gift { animation: none !important; } }



        .confetti { position: fixed; top: -12px; width: 9px; height: 14px; z-index: 3; pointer-events: none; animation: fall linear forwards; }

        @keyframes fall { to { transform: translateY(110vh) rotate(720deg); } }

        @media (prefers-reduced-motion: reduce) { .confetti { display: none; } .modal { animation: none; } }



        @media (max-width: 820px) {

            .body { flex-direction: column; }

            .aside { width: auto; border-left: 0; border-top: 1px solid var(--line); }

            .dots { grid-template-columns: repeat(10, 1fr); }

        }

    </style>
</head>
<body>
<form id="form1" runat="server">
    <div class="app-shell">
        <uc:NavDashboard ID="navDashboard" runat="server" />

        <div class="main">

            <%-- ============ Quiz ============ --%>
            <asp:Panel ID="pnlQuiz" runat="server">
                <div class="topbar">
                    <div class="qz-title">
                        <h1><asp:Literal ID="litQuizTitle" runat="server" /></h1>
                        <span class="qz-badge" id="qBadge">Question 1 of 1</span>
                    </div>
                    <div class="qz-timer" id="timer" aria-live="off">--:-- remaining</div>
                </div>

                <div class="qz-body">
                    <div class="content">
                        <div class="qz-card">
                            <div class="qz-label" id="qLabel"></div>
                            <h2 class="qz-text" id="qText"></h2>
                            <div id="qOptions" role="radiogroup" aria-labelledby="qText"></div>
                            <div class="qz-nav">
                                <button type="button" class="btn btn-outline" id="btnPrev">Previous Question</button>
                                <button type="button" class="btn" id="btnNext">Next Question</button>
                            </div>
                        </div>
                    </div>

                    <div class="qz-aside">
                        <h3>Assessment Progress</h3>
                        <div class="qz-dots" id="dots"></div>
                        <button type="button" class="btn btn-green btn-block" id="btnFinish">Submit Quiz</button>
                    </div>
                </div>

                <asp:HiddenField ID="hfAnswers" runat="server" />
                <asp:Button ID="btnSubmit" runat="server" Text="Submit" OnClick="btnSubmit_Click"
                            Style="display:none" UseSubmitBehavior="false" />
            </asp:Panel>

            <%-- ============ Error ============ --%>
            <asp:Panel ID="pnlError" runat="server" Visible="false">
                <div class="qz-error">
                    <h2>We couldn't open this quiz</h2>
                    <p><asp:Literal ID="litError" runat="server" /></p>
                    <a class="btn" href="<%= ResolveUrl("~/pages/General/coursesDirectory.aspx") %>">Back to courses</a>
                </div>
            </asp:Panel>

            <%-- ============ Result + review ============ --%>
            <asp:Panel ID="pnlResult" runat="server" Visible="false">
                <div class="topbar">
                    <div class="qz-title">
                        <h1>Answer Review</h1>
                        <span class="qz-badge"><asp:Literal ID="litReviewSummary" runat="server" /></span>
                    </div>
                    <div class="qz-actions">
                        <button type="button" class="btn btn-outline" id="btnShowResult">View Result</button>
                        <a class="btn btn-outline" href="<%= Request.Url.AbsolutePath %>?quiz_id=<%= QuizIdForLinks %>">Retake Quiz</a>
                        <a class="btn" href="<%= LessonUrl %>">Back to Lesson</a>
                    </div>
                </div>

                <div class="content" id="review" tabindex="-1">
                    <asp:Literal ID="litReview" runat="server" />
                </div>

                <div class="qz-overlay" id="resultOverlay">
                    <div class="qz-modal g-<%= ResultGrade %>" data-grade="<%= ResultGrade %>" role="dialog" aria-modal="true" aria-labelledby="resTitle">
                        <div class="qz-gift" aria-hidden="true"><%= ResultIcon %></div>
                        <h2 id="resTitle"><%= ResultTitle %></h2>
                        <div class="qz-grade"><asp:Literal ID="litGrade" runat="server" /></div>
                        <div class="qz-score"><asp:Literal ID="litScore" runat="server" /></div>
                        <asp:Literal ID="litVerdict" runat="server" />
                        <p class="qz-note"><asp:Literal ID="litSaveNote" runat="server" /></p>
                        <div class="qz-actions">
                            <button type="button" class="btn btn-green" id="btnReview">Review Answers</button>
                            <a class="btn btn-outline" href="<%= LessonUrl %>">Back to Lesson</a>
                            <a class="btn btn-outline" href="<%= Request.Url.AbsolutePath %>?quiz_id=<%= QuizIdForLinks %>">Retake Quiz</a>
                        </div>
                    </div>
                </div>
            </asp:Panel>

        </div>
    </div>

    <%-- Questions are sent WITHOUT the correct answer; grading happens on the server. --%>
    <asp:Literal ID="litScript" runat="server" />
</form>

<script>
(function () {
    // ---- Result page: celebration depends on the grade ----
    var modal = document.querySelector('.modal[data-grade], .qz-modal[data-grade]');
    if (modal && !window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
        var plans = {
            A: { count: 150, colors: ['#ffc400', '#ffd54a', '#ff7043', '#e040fb', '#29b6f6', '#66bb6a'], time: 2.2 },
            B: { count: 80, colors: ['#e53935', '#8e24aa', '#1e88e5', '#fb8c00', '#00bfa5', '#fdd835'], time: 3 },
            C: { count: 35, colors: ['#42a5f5', '#7e57c2', '#26c6da', '#9ccc65'], time: 3.8 },
            D: { count: 12, colors: ['#ffb067', '#ffa726', '#ffcc80'], time: 6, star: true },
            F: { count: 0, colors: [], time: 0 }   // no confetti, just a calm dialog
        };
        var plan = plans[modal.getAttribute('data-grade')] || plans.F;
        for (var i = 0; i < plan.count; i++) {
            var c = document.createElement('div');
            c.className = 'qz-confetti';
            c.style.left = Math.random() * 100 + 'vw';
            c.style.animationDuration = (plan.time + Math.random() * 2.5) + 's';
            c.style.animationDelay = (Math.random() * 1.2) + 's';
            if (plan.star) {
                c.textContent = '\u2726';
                c.style.color = plan.colors[i % plan.colors.length];
                c.style.width = 'auto'; c.style.height = 'auto'; c.style.fontSize = (14 + Math.random() * 10) + 'px';
            } else {
                c.style.background = plan.colors[i % plan.colors.length];
                c.style.borderRadius = Math.random() > .5 ? '50%' : '2px';
            }
            document.body.appendChild(c);
        }
    }

    // ---- Result dialog: review / reopen ----
    var overlay = document.getElementById('resultOverlay');
    if (overlay) {
        document.getElementById('btnReview').onclick = function () {
            overlay.style.display = 'none';
            var bits = document.querySelectorAll('.qz-confetti');
            for (var k = 0; k < bits.length; k++) bits[k].parentNode.removeChild(bits[k]);
            var rv = document.getElementById('review');
            rv.scrollIntoView({ behavior: 'smooth', block: 'start' });
            rv.focus({ preventScroll: true });
        };
        document.getElementById('btnShowResult').onclick = function () { overlay.style.display = 'grid'; };
    }

    // ---- Quiz runner ----
    if (typeof QUIZ === 'undefined') return;

    var qs = QUIZ.questions, answers = {}, cur = 0, remaining = QUIZ.seconds, submitted = false;
    var $ = function (id) { return document.getElementById(id); };

    function render() {
        var q = qs[cur];
        $('qBadge').textContent = 'Question ' + (cur + 1) + ' of ' + qs.length;
        $('qLabel').textContent = 'Question ' + (cur + 1);
        $('qText').textContent = q.text;

        var box = $('qOptions');
        box.innerHTML = '';
        q.options.forEach(function (o) {
            var b = document.createElement('button');
            b.type = 'button';
            b.className = 'qz-opt' + (answers[q.id] === o.key ? ' selected' : '');
            b.setAttribute('role', 'radio');
            b.setAttribute('aria-checked', answers[q.id] === o.key ? 'true' : 'false');
            var k = document.createElement('span'); k.className = 'qz-key'; k.textContent = o.key;
            var t = document.createElement('span'); t.textContent = o.text;
            b.appendChild(k); b.appendChild(t);
            b.onclick = function () { answers[q.id] = o.key; render(); };
            box.appendChild(b);
        });

        $('btnPrev').disabled = cur === 0;
        $('btnNext').disabled = cur === qs.length - 1;

        var dots = $('dots');
        dots.innerHTML = '';
        qs.forEach(function (x, i) {
            var d = document.createElement('button');
            d.type = 'button';
            d.className = 'qz-dot' + (answers[x.id] ? ' answered' : '') + (i === cur ? ' current' : '');
            d.textContent = i + 1;
            d.setAttribute('aria-label', 'Go to question ' + (i + 1) + (answers[x.id] ? ' (answered)' : ''));
            d.onclick = function () { cur = i; render(); };
            dots.appendChild(d);
        });
    }

    function tick() {
        var m = Math.floor(remaining / 60), s = remaining % 60;
        $('timer').textContent = (m < 10 ? '0' : '') + m + ':' + (s < 10 ? '0' : '') + s + ' remaining';
        $('timer').className = 'qz-timer' + (remaining <= 60 ? ' low' : '');
        if (remaining <= 0) { send(); return; }
        remaining--;
    }

    function send() {
        if (submitted) return;
        submitted = true;
        clearInterval(timerId);
        document.getElementById(QUIZ.hiddenId).value = JSON.stringify(answers);
        document.getElementById(QUIZ.buttonId).click();
    }

    $('btnPrev').onclick = function () { if (cur > 0) { cur--; render(); } };
    $('btnNext').onclick = function () { if (cur < qs.length - 1) { cur++; render(); } };
    $('btnFinish').onclick = function () {
        var left = qs.length - Object.keys(answers).length;
        var msg = left > 0
            ? 'You have ' + left + ' unanswered question' + (left > 1 ? 's' : '') + '. Submit anyway?'
            : 'Submit your quiz now?';
        if (confirm(msg)) send();
    };

    render();
    tick();
    var timerId = setInterval(tick, 1000);
})();
</script>
</body>
</html>