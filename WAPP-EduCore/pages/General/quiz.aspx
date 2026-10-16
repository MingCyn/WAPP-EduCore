<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="quiz.aspx.cs" Inherits="WAPP_EduCore.pages.General.quiz" %>
<%@ Register Src="~/pages/Shared/navDashboard.ascx" TagPrefix="uc" TagName="NavDashboard" %>

<!DOCTYPE html>
<html lang="en">
    <head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Quiz | EduCore</title>
    <link rel="stylesheet" href="/Content/course.css" />
    <link rel="stylesheet" href="/Content/quiz.css" />
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