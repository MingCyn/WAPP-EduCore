<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="savedNotes.aspx.cs" Inherits="WAPP_EduCore.pages.Student.SavedNotes" %>
<%@ Register Src="~/pages/Shared/Sidebar.ascx" TagName="Sidebar" TagPrefix="uc" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Course Study Notebooks</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet" />
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/student.css" rel="stylesheet" type="text/css" />
    <link href="/Content/student-upgrade.css" rel="stylesheet" />
    <script src="/Content/fx.js"></script>
    <style>
        .sn-wrap { max-width: 100%; width: 100%; margin: 0 auto; padding: 0 10px; box-sizing: border-box; }
        
        /* Course Row Card Layout */
        .course-row-card {
            background: #ffffff; border: 1px solid #e2e8f0; border-radius: 16px;
            display: flex; align-items: center; padding: 18px 24px; margin-bottom: 16px;
            width: 100%; box-sizing: border-box; box-shadow: 0 2px 6px rgba(0,0,0,0.03);
            text-decoration: none; transition: all 0.2s ease;
        }
        .course-row-card:hover { border-color: #4f46e5; box-shadow: 0 4px 14px rgba(79, 70, 229, 0.08); transform: translateY(-2px); }
        .course-card-img-wrap { width: 80px; height: 80px; border-radius: 12px; background: #f8fafc; border: 1px solid #e2e8f0; overflow: hidden; flex-shrink: 0; margin-right: 24px; display: flex; align-items: center; justify-content: center; }
        .course-card-img-wrap img { width: 100%; height: 100%; object-fit: cover; }
        .course-card-info { flex-grow: 1; }
        .course-card-title { font-size: 17px; font-weight: 700; color: #1e293b; margin: 0 0 6px 0; }
        .course-card-sub { font-size: 14px; color: #64748b; margin: 0; font-weight: 500; }
        .course-card-action { font-weight: 600; color: #4f46e5; font-size: 15px; display: flex; align-items: center; gap: 6px; padding-left: 20px; }

        /* Note details view styling */
        .notebook-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; flex-wrap: wrap; gap: 12px; background:#fff; padding:18px 24px; border-radius:12px; border:1px solid #e2e8f0; width: 100%; box-sizing: border-box; }
        .sn-card { background: #fff; border: 1px solid #e2e8f0; border-radius: 12px; padding: 20px 24px; margin-bottom: 16px; width: 100%; box-sizing: border-box; box-shadow: 0 1px 3px rgba(0,0,0,0.03); }
        .sn-meta { display: flex; justify-content: space-between; gap: 12px; flex-wrap: wrap; font-size: 13px; color: #64748b; margin-bottom: 10px; }
        .sn-meta a { color: #4f46e5; font-weight: 600; text-decoration: none; }
        .sn-meta a:hover { text-decoration: underline; }
        .sn-quote { font-size: 13.5px; color: #334155; background: #f8fafc; border-left: 4px solid #4f46e5; padding: 10px 14px; border-radius: 4px; margin: 0 0 12px; max-height: 110px; overflow-y: auto; white-space: pre-wrap; }
        .sn-body { font-size: 15px; line-height: 1.65; color: #1e293b; }
        .sn-body p { margin: 0 0 8px; }
        .sn-body ul { margin: 4px 0 10px 20px; padding: 0; }
        .sn-body h4 { margin: 12px 0 4px; font-size: 15px; }
        .sn-actions { margin-top: 12px; text-align: right; }
        .sn-del { background: none; border: 0; color: #b91c1c; font-size: 13.5px; font-weight: 600; cursor: pointer; padding: 4px 6px; }
        .sn-del:hover { text-decoration: underline; }
        .sn-empty { text-align: center; padding: 50px 20px; }
        
        /* Flashcard Modal & Card Deck Styling */
        #flashcardModal { display: none; position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.6); z-index: 2000; align-items: center; justify-content: center; }
        .flashcard-box { background: #fff; width: 650px; max-width: 92%; padding: 28px; border-radius: 16px; box-shadow: 0 15px 35px rgba(0,0,0,0.25); display: flex; flex-direction: column; }
        
        /* 3D Flip Card */
        .fc-deck-container { margin: 20px 0; perspective: 1000px; display: flex; justify-content: center; }
        .flashcard-item { width: 100%; height: 280px; position: relative; transform-style: preserve-3d; transition: transform 0.6s cubic-bezier(0.4, 0.2, 0.2, 1); cursor: pointer; border-radius: 14px; box-shadow: 0 8px 20px rgba(0,0,0,0.08); }
        .flashcard-item.flipped { transform: rotateY(180deg); }
        .fc-face { position: absolute; width: 100%; height: 100%; backface-visibility: hidden; border-radius: 14px; padding: 30px; box-sizing: border-box; display: flex; flex-direction: column; justify-content: center; align-items: center; text-align: center; }
        
        .fc-front { background: linear-gradient(135.5deg, #fef08a 0%, #fde047 100%); color: #713f12; border: 2px solid #facc15; }
        .fc-back { background: #ffffff; color: #1e293b; border: 2px solid #cbd5e1; transform: rotateY(180deg); }
        
        .fc-badge { font-size: 11px; text-transform: uppercase; font-weight: 700; letter-spacing: 1px; padding: 4px 10px; border-radius: 20px; margin-bottom: 15px; background: rgba(0,0,0,0.06); }
        .fc-text { font-size: 17px; font-weight: 600; line-height: 1.5; margin: 0; max-height: 160px; overflow-y: auto; width: 100%; }
        
        /* Navigation Controls */
        .fc-nav { display: flex; justify-content: space-between; align-items: center; margin-top: 10px; }
        .fc-btn { background: #f1f5f9; border: 1px solid #cbd5e1; color: #334155; padding: 8px 16px; border-radius: 8px; font-weight: 600; cursor: pointer; transition: all 0.2s; }
        .fc-btn:hover:not(:disabled) { background: #e2e8f0; color: #0f172a; }
        .fc-btn:disabled { opacity: 0.4; cursor: not-allowed; }
        .fc-counter { font-size: 14px; font-weight: 600; color: #64748b; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <uc:Sidebar ID="Sidebar1" runat="server" />
            <main class="main">
                <div class="topbar">
                    <div class="lv-heading">
                        <span class="title">
                            <asp:Literal ID="litTopTitle" runat="server" Text="📚 Course Study Notebooks"></asp:Literal>
                        </span>
                    </div>
                    <div class="lv-top-right">
                        <asp:HyperLink ID="lnkBack" runat="server" NavigateUrl="~/pages/Student/myCourses.aspx" CssClass="btn-ghost">Back to Courses</asp:HyperLink>
                    </div>
                </div>

                <div class="sn-wrap">
                    <!-- VIEW 1: Full-width Course Row Cards -->
                    <asp:Panel ID="pnlCoursesList" runat="server" Visible="true">
                        <div style="margin-bottom: 20px;">
                            <h3 style="margin:0 0 4px 0; color:#1e293b; font-size:18px;">Select a Course Notebook</h3>
                            <p style="margin:0; color:#64748b; font-size:14px;">Choose a course below to view your saved notes and generate study flashcards.</p>
                        </div>
                        
                        <asp:Repeater ID="rptCourses" runat="server">
                            <ItemTemplate>
                                <a href='<%# "savedNotes.aspx?course=" + Eval("course_id") %>' class="course-row-card">
                                    <div class="course-card-img-wrap">
                                        <img src='<%# ResolveCourseImage(Eval("course_img")) %>' alt="Course Image" onerror="this.src='../../Content/images/default-course.png';" />
                                    </div>
                                    <div class="course-card-info">
                                        <h4 class="course-card-title"><%#: Eval("notebook_title") %></h4>
                                        <p class="course-card-sub"><%# Eval("note_count") %> saved highlight note(s)</p>
                                    </div>
                                    <div class="course-card-action">
                                        View Notes &rarr;
                                    </div>
                                </a>
                            </ItemTemplate>
                        </asp:Repeater>
                    </asp:Panel>

                    <!-- VIEW 2: Detailed Notes List for a Selected Course -->
                    <asp:Panel ID="pnlNotesList" runat="server" Visible="false">
                        <div class="notebook-header">
                            <div>
                                <h3 id="lblNotebookTitle" runat="server" style="margin:0 0 4px 0; color:#1e293b; font-size:17px;">Notebook Notes</h3>
                                <span class="sn-count" style="color:#64748b; font-size:13px;"><asp:Literal ID="litCount" runat="server"></asp:Literal></span>
                            </div>
                            <div style="display:flex; gap:10px;">
                                <asp:HyperLink ID="lnkViewAllCourses" runat="server" NavigateUrl="savedNotes.aspx" CssClass="btn-ghost" style="padding:8px 14px; font-size:13px;">&larr; All Courses</asp:HyperLink>
                                <!-- Orange Generate Flashcards Button -->
                                <button type="button" id="btnGenerateFlashcards" class="btn-solid" style="background:#f97316; color:white; padding:8px 18px; border-radius:6px; border:none; font-weight:700; cursor:pointer; box-shadow:0 2px 5px rgba(249,115,22,0.3);">
                                    ✨ Generate Flashcards
                                </button>
                            </div>
                        </div>

                        <asp:Repeater ID="rptNotes" runat="server" OnItemCommand="rptNotes_ItemCommand">
                            <ItemTemplate>
                                <div class="sn-card">
                                    <div class="sn-meta">
                                        <span>Lesson: <a href='<%# LessonLink(Eval("course_id"), Eval("lesson_id")) %>'><%#: Eval("lesson_title") %></a></span>
                                        <span><%# Eval("created_at", "{0:dd MMM yyyy, HH:mm}") %></span>
                                    </div>
                                    <div class="sn-quote"><%#: Eval("highlighted_text") %></div>
                                    <div class="sn-body"><%# FormatNote(Eval("ai_summary")) %></div>
                                    <div class="sn-actions">
                                        <asp:LinkButton ID="btnDelete" runat="server" CssClass="sn-del" CommandName="delete"
                                            CommandArgument='<%# Eval("note_id") %>'
                                            OnClientClick="return confirm('Delete this highlight note?');">Delete note</asp:LinkButton>
                                    </div>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </asp:Panel>

                    <!-- Empty State Panel -->
                    <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="result-box sn-empty">
                        <h3>No notebook notes found</h3>
                        <p>Open a course lesson, highlight key text passages, and create AI study notes to populate your notebooks.</p>
                        <p style="margin-top:12px"><a href="myCourses.aspx" class="btn-solid">Go to my courses</a></p>
                    </asp:Panel>
                </div>
            </main>
        </div>

        <!-- Flashcard Modal Dialog with Card Deck & Navigation -->
        <div id="flashcardModal">
            <div class="flashcard-box">
                <div style="display:flex; justify-content:space-between; align-items:center;">
                    <h3 style="margin:0; color:#1e293b; font-size:18px;">✨ Interactive Flashcards</h3>
                    <button type="button" id="btnCloseFlashcard" class="btn-ghost" style="padding:4px 10px; cursor:pointer;">✕ Close</button>
                </div>
                
                <div id="flashcardDeckContainer" style="display:none;">
                    <div class="fc-deck-container">
                        <div class="flashcard-item" id="activeFlashcard" onclick="toggleFlipCard()">
                            <div class="fc-face fc-front">
                                <span class="fc-badge">Question (Click to Flip)</span>
                                <p class="fc-text" id="fcQuestionText"></p>
                            </div>
                            <div class="fc-face fc-back">
                                <span class="fc-badge" style="background:#e0e7ff; color:#3730a3;">Answer</span>
                                <p class="fc-text" id="fcAnswerText"></p>
                            </div>
                        </div>
                    </div>

                    <div class="fc-nav">
                        <button type="button" class="fc-btn" id="btnPrevCard" onclick="navigateCard(-1)">&larr; Previous</button>
                        <span class="fc-counter" id="fcCounterText">Card 1 of 1</span>
                        <button type="button" class="fc-btn" id="btnNextCard" onclick="navigateCard(1)">Next &rarr;</button>
                    </div>
                    
                    <div style="text-align:center; margin-top:15px;">
                        <button type="button" class="fc-btn" style="background:#ffedd5; border-color:#fdba74; color:#9a3412; font-weight:700;" onclick="toggleFlipCard()">🔄 Flip Card</button>
                    </div>
                </div>

                <div id="flashcardLoading" style="text-align:center; padding:50px 20px; color:#64748b; font-weight:500;">
                    Generating high-yield flashcards from your notebook...
                </div>
            </div>
        </div>

<script type="text/javascript">
            var activeCourseId = "<%= _selectedCourseId %>";
            
            /** @type {any[]} */
            var flashcardDeck = [];
            var currentCardIndex = 0;

            var btnGen = document.getElementById("btnGenerateFlashcards");
            if (btnGen) {
                btnGen.addEventListener("click", function () {
                    var modal = document.getElementById("flashcardModal");
                    var loadingDiv = document.getElementById("flashcardLoading");
                    var deckContainer = document.getElementById("flashcardDeckContainer");
                    
                    if (modal) modal.style.display = "flex";
                    if (loadingDiv) { loadingDiv.style.display = "block"; loadingDiv.innerHTML = "Generating flashcards from your notebook notes..."; }
                    if (deckContainer) deckContainer.style.display = "none";

                    fetch('/pages/Student/GenerateFlashcards.ashx', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ courseId: activeCourseId })
                    })
                    .then(function (res) { return res.json(); })
                    .then(function (data) {
                        if (data.success) {
                            flashcardDeck = parseFlashcards(String(data.flashcards));
                            if (flashcardDeck.length > 0) {
                                currentCardIndex = 0;
                                renderCurrentCard();
                                if (loadingDiv) loadingDiv.style.display = "none";
                                if (deckContainer) deckContainer.style.display = "block";
                            } else {
                                if (loadingDiv) loadingDiv.innerHTML = "<span style='color:red;'>No valid flashcards could be parsed from AI response.</span>";
                            }
                        } else {
                            if (loadingDiv) loadingDiv.innerHTML = '<span style="color:red;">' + (data.message || "Error generating flashcards.") + '</span>';
                        }
                    })
                    .catch(function (err) {
                        if (loadingDiv) loadingDiv.innerHTML = "<span style='color:red;'>Could not connect to AI service.</span>";
                    });
                });
            }

            /** @param {string} rawText */
            function parseFlashcards(rawText) {
                /** @type {any[]} */
                var cards = [];
                var rawCards = rawText.split(/### Card \d+/);
                for (var i = 0; i < rawCards.length; i++) {
                    var block = rawCards[i].trim();
                    if (!block) continue;

                    var q = "Review Concept";
                    var a = block;

                    if (block.indexOf("**Question:**") !== -1 && block.indexOf("**Answer:**") !== -1) {
                        var parts = block.split("**Answer:**");
                        q = parts[0].replace("**Question:**", "").trim();
                        a = parts[1].trim();
                    }
                    cards.push({ question: q, answer: a });
                }
                return cards;
            }

            function renderCurrentCard() {
                if (flashcardDeck.length === 0) return;
                /** @type {any} */
                var card = flashcardDeck[currentCardIndex];
                
                var qText = document.getElementById("fcQuestionText");
                var aText = document.getElementById("fcAnswerText");
                var cText = document.getElementById("fcCounterText");
                var activeCard = document.getElementById("activeFlashcard");

                if (qText) qText.innerText = card.question;
                if (aText) aText.innerHTML = card.answer.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>').replace(/\n/g, '<br/>');
                if (cText) cText.innerText = "Card " + (currentCardIndex + 1) + " of " + flashcardDeck.length;

                if (activeCard) activeCard.classList.remove("flipped");

                var btnPrev = document.getElementById("btnPrevCard");
                var btnNext = document.getElementById("btnNextCard");
                
                if (btnPrev && "disabled" in btnPrev) {
                    /** @type {any} */ (btnPrev).disabled = (currentCardIndex === 0);
                }
                if (btnNext && "disabled" in btnNext) {
                    /** @type {any} */ (btnNext).disabled = (currentCardIndex === flashcardDeck.length - 1);
                }
            }

            /** @param {number} direction */
            function navigateCard(direction) {
                currentCardIndex += direction;
                if (currentCardIndex < 0) currentCardIndex = 0;
                if (currentCardIndex >= flashcardDeck.length) currentCardIndex = flashcardDeck.length - 1;
                renderCurrentCard();
            }

            function toggleFlipCard() {
                var card = document.getElementById("activeFlashcard");
                if (card) card.classList.toggle("flipped");
            }

            var btnClose = document.getElementById("btnCloseFlashcard");
            if (btnClose) {
                btnClose.addEventListener("click", function () {
                    var modal = document.getElementById("flashcardModal");
                    if (modal) modal.style.display = "none";
                });
            }
</script>
    </form>
</body>
</html>