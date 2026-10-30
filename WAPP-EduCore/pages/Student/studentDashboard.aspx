<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="studentDashboard.aspx.cs"
    Inherits="WAPP_EduCore.pages.Student.WebForm2" %>

<%@ Register Src="~/pages/Shared/Sidebar.ascx" TagName="Sidebar" TagPrefix="uc" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Student Dashboard</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Merriweather:ital,wght@0,400;0,700;1,400&display=swap"
        rel="stylesheet" />
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/student.css" rel="stylesheet" type="text/css" />
    <link href="/Content/student-upgrade.css" rel="stylesheet" />
    <style>
        a.course-card{
            display: flex;
            flex-direction: column; /* Stacks thumb and meta vertically */
            height: 100%;           /* Forces card to fill the grid row height */
            color: inherit;
            text-decoration: none;
            background: #fcf6ee;    /* Sets a uniform background color for the whole card */
        }
        a.course-card:hover{box-shadow:0 8px 22px rgba(16,24,40,0.12)}
        
        .course-card .meta {
            flex: 1;                /* Forces meta area to expand and fill remaining space */
            display: flex;
            flex-direction: column;
            background: #fcf6ee;    /* Ensures meta background matches */
        }

        .course-card .progress {
            margin-top: auto;       /* Pushes the progress bar cleanly to the bottom */
        }

        .courses {
            display: grid;
            grid-template-columns: repeat(3, 1fr);
            gap: 18px;
            align-items: stretch;   /* Ensures all cards stretch to match the tallest card in the row */
        }
        
        @media (max-width: 1300px) {
            .courses {
                grid-template-columns: repeat(2, 1fr);
            }
        }

        .db-empty{padding:26px 10px;font-size:13px;color:#7b8697}

        /* ---------- fonts: same serif headings as the other student pages ---------- */
        .topbar .greeting{font-family:'Merriweather',Georgia,serif;font-size:26px;font-weight:700;color:#111c3d}
        .hero h2{font-family:'Merriweather',Georgia,serif;font-size:28px;font-weight:700;line-height:1.3}
        .section-title{font-family:'Merriweather',Georgia,serif;font-size:19px;font-weight:700;color:#111c3d}
        .section-title a{font-family:'Inter','Segoe UI',Arial,sans-serif;font-size:13px;font-weight:500}
        .course-card h4{font-family:'Merriweather',Georgia,serif;font-size:16px;font-weight:700;line-height:1.3;color:#111c3d}
        .right-col .card h4{font-family:'Merriweather',Georgia,serif;font-size:17px;font-weight:700;color:#111c3d}
        .stat .num{font-family:'Merriweather',Georgia,serif;font-size:24px;font-weight:700;color:#243f86}
        .deadline-item strong,.achievements strong{font-weight:600;color:#111c3d}

        /* ---------- Today's Thought quote card ---------- */
        .hero .thought{
            position:relative;
            overflow:hidden;
            flex:0 1 400px;
            min-width:280px;
            padding:22px 26px 18px;
            background:linear-gradient(145deg,#fcf3e7 0%,#f4e0c8 100%);
            border:1px solid #ecd5b8;
            border-radius:16px;
            color:#1f2a44;
            box-shadow:0 12px 32px rgba(0,0,0,.28), inset 0 1px 0 rgba(255,255,255,.7);
        }
        .qt-mark{
            position:absolute;
            top:-14px;
            right:16px;
            font-family:'Merriweather',Georgia,serif;
            font-size:120px;
            line-height:1;
            color:#e0a85a;
            opacity:.35;
            pointer-events:none;
            user-select:none;
        }
        .qt-label{
            display:flex;
            align-items:center;
            gap:8px;
            font-family:'Inter','Segoe UI',Arial,sans-serif;
            font-size:11px;
            font-weight:700;
            letter-spacing:1.4px;
            text-transform:uppercase;
            color:#a0662a;
        }
        .qt-label::before{
            content:"";
            width:8px;height:8px;
            border-radius:50%;
            background:#e0a85a;
            box-shadow:0 0 0 3px rgba(224,168,90,.25);
        }
        .qt-body{
            transition:opacity .22s ease, transform .22s ease;
        }
        .qt-body.is-fading{opacity:0;transform:translateY(5px)}
        .qt-text{
            margin:14px 0 18px;
            font-family:'Merriweather',Georgia,serif;
            font-style:italic;
            font-size:16px;
            line-height:1.65;
            color:#1f2a44;
        }
        .qt-foot{
            display:flex;
            align-items:center;
            gap:10px;
        }
        .qt-line{width:24px;height:2px;border-radius:2px;background:#e0a85a;flex-shrink:0}
        .qt-author{
            flex:1;
            min-width:0;
            font-family:'Inter','Segoe UI',Arial,sans-serif;
            font-size:13px;
            font-weight:600;
            color:#5b4a35;
        }
        .qt-btn{
            display:inline-flex;
            align-items:center;
            justify-content:center;
            flex-shrink:0;
            width:34px;height:34px;
            padding:0;
            border:1px solid #e3c9a6;
            border-radius:50%;
            background:rgba(255,255,255,.65);
            color:#8a5a22;
            cursor:pointer;
            transition:background .2s ease, transform .2s ease, border-color .2s ease;
        }
        .qt-btn:hover{background:#fff;border-color:#d9a867}
        .qt-btn:focus-visible{outline:2px solid #243f86;outline-offset:2px}
        .qt-btn svg{width:16px;height:16px;transition:transform .45s ease}
        .qt-btn:hover svg{transform:rotate(180deg)}
        .qt-btn:active{transform:scale(.92)}

        @media (prefers-reduced-motion: reduce){
            .qt-body,.qt-btn,.qt-btn svg{transition:none}
        }
        @media (max-width: 900px){
            .hero{flex-direction:column;align-items:stretch}
            .hero .thought{flex:1 1 auto;max-width:none}
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <uc:Sidebar ID="Sidebar1" runat="server" />
            <main class="main">
                <div class="topbar">
                    <div class="greeting">
                        <asp:Literal ID="litGreeting" runat="server" Mode="Encode"></asp:Literal></div>
                </div>

                <div class="hero">
                    <div style="flex: 1">
                        <h2>Learn your way, on your schedule.</h2>
                        <p>
                            <asp:Literal ID="litHeroText" runat="server" Mode="Encode"></asp:Literal></p>
                        <asp:HyperLink ID="lnkContinue" runat="server" CssClass="btn"
                            Style="display: inline-block; text-decoration: none" NavigateUrl="~/pages/Student/myCourses.aspx">Continue Learning</asp:HyperLink>
                    </div>

                    <!-- Today's Thought: the quote below is only a fallback. The script at the bottom of the page replaces it with a random one. -->
                    <div class="thought" id="quoteCard">
                        <span class="qt-mark" aria-hidden="true">&ldquo;</span>
                        <div class="qt-label">Today&rsquo;s Thought</div>
                        <div class="qt-body" id="quoteBody" aria-live="polite">
                            <div class="qt-text" id="quoteText">&ldquo;The beautiful thing about learning is that
                                no one can take it away from you.&rdquo;</div>
                            <div class="qt-foot">
                                <span class="qt-line" aria-hidden="true"></span>
                                <span class="qt-author" id="quoteAuthor">B.B. King</span>
                                <button type="button" class="qt-btn" id="btnNewQuote" title="Show another quote"
                                    aria-label="Show another quote">
                                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"
                                        stroke-linejoin="round" aria-hidden="true">
                                        <polyline points="23 4 23 10 17 10"></polyline>
                                        <polyline points="1 20 1 14 7 14"></polyline>
                                        <path d="M3.5 9a9 9 0 0 1 14.9-3.4L23 10M1 14l4.6 4.4A9 9 0 0 0 20.5 15"></path>
                                    </svg>
                                </button>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="content">
                    <div class="left-col">
                        <div>
                            <div class="section-title">
                                <div>My Active Courses</div>
                                <a href="myCourses.aspx" style="font-size: 13px; color: #3561b3">View all courses</a>
                            </div>

                            <asp:Panel ID="pnlNoCourses" runat="server" Visible="false" CssClass="db-empty">
                                <asp:Literal ID="litNoCourses" runat="server" Mode="Encode"></asp:Literal>
                            </asp:Panel>

                            <div class="courses">
                                <asp:Repeater ID="rptCourses" runat="server">
                                    <ItemTemplate>
                                        <a class="course-card" href='<%# Eval("Url") %>'>
                                            <div class="thumb" style="<%# Eval("ThumbStyle") %>"></div>
                                            <div class="meta">
                                                <h4><%#: Eval("Title") %></h4>
                                                <small>Tutor: <%#: Eval("Tutor") %></small>
                                                <div class="progress"><span style='<%# "width: " + Eval("Percent") + "%;" %>'></span>
                                                </div>
                                                <small><%# Eval("Percent") %>% complete</small>
                                            </div>
                                        </a>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </div>
                        </div>

                        <div class="stats">
                            <div class="stat">
                                <small>Lessons This Week</small>
                                <div class="num">
                                    <asp:Literal ID="litWeek" runat="server"></asp:Literal></div>
                                <small style="color: #9aa6b8">
                                    <asp:Literal ID="litWeekNote" runat="server" Mode="Encode"></asp:Literal></small>
                            </div>
                            <div class="stat">
                                <small>Current Streak</small>
                                <div class="num">
                                    <asp:Literal ID="litStreak" runat="server"></asp:Literal></div>
                                <small style="color: #9aa6b8">
                                    <asp:Literal ID="litStreakNote" runat="server" Mode="Encode"></asp:Literal></small>
                            </div>
                            <div class="stat">
                                <small>Active Courses</small>
                                <div class="num">
                                    <asp:Literal ID="litActive" runat="server"></asp:Literal></div>
                                <small style="color: #9aa6b8">
                                    <asp:Literal ID="litActiveNote" runat="server" Mode="Encode"></asp:Literal></small>
                            </div>
                        </div>
                    </div>

                    <div class="right-col">
                        <div class="card">
                            <h4 style="margin-bottom: 8px">Upcoming Deadlines</h4>
                            <asp:Panel ID="pnlNoDeadlines" runat="server" Visible="false" CssClass="db-empty">
                                No upcoming deadlines. Add one in your calendar.
                           
                            </asp:Panel>
                            <div class="deadline">
                                <asp:Repeater ID="rptDeadlines" runat="server">
                                    <ItemTemplate>
                                        <div class="deadline-item">
                                            <div>
                                                <strong><%#: Eval("Title") %></strong>
                                                <div style="color: #7b8697; font-size: 13px"><%#: Eval("Sub") %></div>
                                            </div>
                                            <div class="badge"><%#: Eval("Badge") %></div>
                                        </div>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </div>
                        </div>

                        <div class="card" style="margin-top: 16px">
                            <h4 style="margin-bottom: 8px">Recent Achievements</h4>
                            <asp:Panel ID="pnlNoAch" runat="server" Visible="false" CssClass="db-empty">
                                Finish a course or ace a quiz to earn your first achievement.
                           
                            </asp:Panel>
                            <ul class="achievements" style="list-style: none">
                                <asp:Repeater ID="rptAch" runat="server">
                                    <ItemTemplate>
                                        <li>
                                            <div style='<%# "width:36px;height:36px;border-radius:8px;background:" + Eval("Tint") + ";" %>'>
                                            </div>
                                            <div>
                                                <strong><%#: Eval("Title") %></strong>
                                                <div style="color: #7b8697; font-size: 13px"><%#: Eval("Sub") %></div>
                                            </div>
                                        </li>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </ul>
                        </div>
                    </div>
                </div>
            </main>
        </div>
    </form>

    <script type="text/javascript">
(function () {
    // Add or remove quotes here. Each entry is [quote, author].
    var QUOTES = [
        ["The beautiful thing about learning is that no one can take it away from you.", "B.B. King"],
        ["Education is the most powerful weapon which you can use to change the world.", "Nelson Mandela"],
        ["I have no special talent. I am only passionately curious.", "Albert Einstein"],
        ["The important thing is not to stop questioning.", "Albert Einstein"],
        ["Intelligence plus character – that is the goal of true education.", "Martin Luther King Jr."],
        ["Education is the passport to the future, for tomorrow belongs to those who prepare for it today.", "Malcolm X"],
        ["The only person who is educated is the one who has learned how to learn and change.", "Carl Rogers"],
        ["Success is the sum of small efforts, repeated day in and day out.", "Robert Collier"],
        ["A journey of a thousand miles begins with a single step.", "Lao Tzu"],
        ["It does not matter how slowly you go as long as you do not stop.", "Confucius"],
        ["The roots of education are bitter, but the fruit is sweet.", "Aristotle"],
        ["The only way to do great work is to love what you do.", "Steve Jobs"],
        ["Genius is one percent inspiration and ninety-nine percent perspiration.", "Thomas Edison"],
        ["You don’t have to be great to start, but you have to start to be great.", "Zig Ziglar"],
        ["The good thing about science is that it’s true whether or not you believe in it.", "Neil deGrasse Tyson"],
        ["Science is a way of thinking much more than it is a body of knowledge.", "Carl Sagan"],
        ["I am still learning.", "Michelangelo"],
        ["The expert in anything was once a beginner.", "Helen Hayes"],
        ["Failure is simply the opportunity to begin again, this time more intelligently.", "Henry Ford"],
        ["What we learn with pleasure we never forget.", "Alfred Mercier"],
        ["The mind is not a vessel to be filled, but a fire to be kindled.", "Plutarch"],
        ["Mistakes are the portals of discovery.", "James Joyce"]
    ];

    var body = document.getElementById('quoteBody');
    var textEl = document.getElementById('quoteText');
    var authorEl = document.getElementById('quoteAuthor');
    var btn = document.getElementById('btnNewQuote');
    if (!body || !textEl || !authorEl) return;

    var current = -1;
    try {
        var last = parseInt(sessionStorage.getItem('lastQuoteIdx'), 10);
        if (!isNaN(last)) current = last;
    } catch (e) { }

    // Random pick that never repeats the quote currently shown
    function nextIndex() {
        var i;
        do { i = Math.floor(Math.random() * QUOTES.length); }
        while (QUOTES.length > 1 && i === current);
        return i;
    }

    function render(i) {
        current = i;
        textEl.textContent = "\u201C" + QUOTES[i][0] + "\u201D";
        authorEl.textContent = QUOTES[i][1];
        try { sessionStorage.setItem('lastQuoteIdx', String(i)); } catch (e) { }
    }

    // First load of the page: pick one straight away (no fade)
    render(nextIndex());

    // Refresh button: fade out, swap, fade in
    if (btn) {
        btn.addEventListener('click', function () {
            body.classList.add('is-fading');
            setTimeout(function () {
                render(nextIndex());
                body.classList.remove('is-fading');
            }, 220);
        });
    }
})();
</script>
</body>
</html>
