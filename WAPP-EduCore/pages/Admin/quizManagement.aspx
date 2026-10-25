<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="quizManagement.aspx.cs" Inherits="WAPP_EduCore.pages.Admin.quizManagement" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">

<head runat="server">
    <title>EduCore - Quiz Management</title>

    <!-- Font -->
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet" />

    <!-- Our own styles (colors, cards, tables, popups...) -->
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />

    <!-- Small extra styles only this page needs (so admin.css does not change) -->
    <style type="text/css">

        /* The little arrow of every dropdown */
        select.input {
            appearance: none;
            -webkit-appearance: none;
            padding-right: 40px;
            background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='%2364748B' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpolyline points='6 9 12 15 18 9'/%3E%3C/svg%3E");
            background-repeat: no-repeat;
            background-position: right 16px center;
            background-size: 14px;
        }

        /* Number boxes with a little icon */
        .stat-top { display: flex; justify-content: space-between; align-items: flex-start; }
        .stat-top svg { flex-shrink: 0; color: var(--blue); }

        /* Sidebar icons */
        .sidebar a.menu-item { display: flex; align-items: center; gap: 12px; }
        .menu-icon { width: 18px; height: 18px; flex-shrink: 0; fill: none; stroke: currentColor; stroke-width: 2; stroke-linecap: round; stroke-linejoin: round; }

        /* The avatar + drop-down menu (top right) */
        .user-menu { position: relative; }
        .user-btn { display: flex; align-items: center; padding: 0; background: none; border: none; cursor: pointer; }
        .user-btn .avatar { margin-left: 0; }
        .user-dropdown { display: none; position: absolute; right: 0; top: 44px; min-width: 180px; padding: 6px; background: white; border: 1px solid var(--line); border-radius: 10px; box-shadow: 0 10px 30px rgba(15, 27, 76, 0.15); z-index: 50; }
        .user-dropdown.open { display: block; }
        .user-dropdown a { display: block; padding: 10px 12px; border-radius: 6px; color: var(--text); font-size: 13px; text-decoration: none; }
        .user-dropdown a:hover { background: #F1F5F9; }
        .user-dropdown a.logout { color: #DC2626; }

        /* Big text box + two small boxes side by side */
        .input.textarea { height: auto; padding: 10px 12px; font-family: inherit; resize: vertical; }
        .form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }

        /* A blue badge */
        .badge.blue { background: #EEF2FF; color: var(--blue); }

        /* The wider popup that shows the questions */
        .modal-box.wide { max-width: 640px; }

        /* ---------- The question cards inside the quiz popup ---------- */
        .q-head { display: flex; justify-content: space-between; align-items: center; margin: 4px 0 10px 0; }
        .q-head h4 { margin: 0; font-size: 14px; }

        .q-card { margin-bottom: 10px; padding: 12px 14px; background: white; border: 1px solid var(--line); border-radius: 10px; }
        .q-top { display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px; font-size: 12px; color: var(--grey); }
        .q-text { margin-bottom: 8px; font-size: 13px; font-weight: 600; line-height: 1.4; }
        .q-opt { padding: 5px 10px; margin-bottom: 4px; border-radius: 6px; background: #F8FAFC; font-size: 12px; }
        .q-opt.correct { background: #DCFCE7; color: #166534; font-weight: 600; }
        .q-explain { margin-top: 6px; color: var(--grey); font-size: 12px; }
        .q-empty { padding: 18px; text-align: center; color: var(--grey); font-size: 13px; background: #F8FAFC; border-radius: 10px; margin-bottom: 14px; }

        /* ---------- Table columns: fixed widths, so the 3 action icons stay on ONE row ---------- */
        table.data { table-layout: fixed; }
        th.c-id      { width: 56px; }
        th.c-quiz    { width: 22%; }
        th.c-lesson  { width: 22%; }
        th.c-course  { width: 19%; }      /* smaller = the Course column is narrower */
        th.c-q       { width: 140px; }
        th.c-pass    { width: 80px; }
        th.c-att     { width: 100px; }
        th.c-actions { width: 150px; }    /* bigger = more room for the icons */
        th.center, td.center { text-align: center; }
        td.actions { white-space: nowrap; }
        .badge { white-space: nowrap; }
    </style>

    <!-- ==================================================
         JAVASCRIPT : controls the popups
         ================================================== -->
    <script type="text/javascript">

var currentId = 0;   // the quiz we clicked on

// ----- Show / hide a popup -----
function openModal(id) { document.getElementById(id).classList.add('open'); }
function closeModal(id) { document.getElementById(id).classList.remove('open'); }

// ----- Helpers -----
function rowOf(id) { return document.getElementById('row' + id); }
function show(id, text) { document.getElementById(id).innerText = text; }
function orDash(text) { return (text == null || text == '') ? '—' : text; }

// Makes a small element with some text  (for example a <div class="q-text">)
function make(tag, cssClass, text) {
    var el = document.createElement(tag);
    if (cssClass) { el.className = cssClass; }
    if (text != null) { el.innerText = text; }
    return el;
}

// ----- POPUP 1 : quiz detail + its questions (when you click a row) -----
function openDetail(id) {
    currentId = id;
    var row = rowOf(id);

    show('dTitle', row.getAttribute('data-title'));
    show('dLesson', orDash(row.getAttribute('data-lesson')));
    show('dCourse', orDash(row.getAttribute('data-course')));
    show('dPass', row.getAttribute('data-pass') + '%');
    show('dAttempts', row.getAttribute('data-attempts'));
    show('dId', id);

    buildQuestions(id, 'dQuestions', 'dQCount', 'detail');
    openModal('detailModal');
}

// Draws the question cards. The questions are hidden in the page (id="qs12"), we read them here.
// boxId = where the cards go,  countId = where "2 questions" goes,  from = 'detail' or 'edit' (the popup we are in)
function buildQuestions(quizId, boxId, countId, from) {
    var box = document.getElementById(boxId);
    box.innerHTML = '';

    var list = document.getElementById('qs' + quizId).children;
    show(countId, list.length + (list.length == 1 ? ' question' : ' questions'));

    if (list.length == 0) {
        box.appendChild(make('div', 'q-empty', 'This quiz has no questions yet. Click "Add Question".'));
        return;
    }

    for (var i = 0; i < list.length; i++) {
        var q = list[i];
        var qid = q.getAttribute('data-id');
        var card = make('div', 'q-card');

        // top line: "Question 1 · Medium · 10 points"  +  edit / delete buttons
        var top = make('div', 'q-top');
        var meta = 'Question ' + (i + 1);
        var dif = q.getAttribute('data-difficulty');
        var pts = q.getAttribute('data-points');
        if (dif != '') { meta += ' · ' + dif; }
        if (pts != '') { meta += ' · ' + pts + (pts == '1' ? ' point' : ' points'); }
        top.appendChild(make('span', '', meta));

        var buttons = make('span');
        var edit = make('button', 'icon-btn', 'Edit');
        edit.type = 'button';
        edit.setAttribute('onclick', 'openQuestion(' + qid + ',' + quizId + ',\'' + from + '\')');
        var del = make('button', 'icon-btn red', 'Delete');
        del.type = 'button';
        del.setAttribute('onclick', 'openQuestionDelete(' + qid + ',' + quizId + ',\'' + from + '\')');
        buttons.appendChild(edit);
        buttons.appendChild(del);
        top.appendChild(buttons);
        card.appendChild(top);

        // the question + the 4 answers (the right one is green)
        card.appendChild(make('div', 'q-text', q.getAttribute('data-text')));

        var letters = ['A', 'B', 'C', 'D'];
        for (var k = 0; k < 4; k++) {
            var option = q.getAttribute('data-' + letters[k].toLowerCase());
            if (option == '') { continue; }
            var isRight = (q.getAttribute('data-correct') == letters[k]);
            card.appendChild(make('div', 'q-opt' + (isRight ? ' correct' : ''), letters[k] + '.  ' + option));
        }

        var why = q.getAttribute('data-explain');
        if (why != '') { card.appendChild(make('div', 'q-explain', 'Explanation: ' + why)); }

        box.appendChild(card);
    }
}

// The "Edit Quiz" button inside the detail popup
function editFromDetail() {
    closeModal('detailModal');
    openEdit(currentId);
}

// The "Add Question" button inside the detail popup
function addQuestionFromDetail() {
    openQuestion(0, currentId, 'detail');
}

// The "+ Add Question" button inside the Edit Quiz popup
function addQuestionFromEdit() {
    openQuestion(0, document.getElementById('hfEditId').value, 'edit');
}

// Go back to the popup we came from
function goBack(from, quizId) {
    if (from == 'edit') { openEdit(quizId); } else { openDetail(quizId); }
}

// ----- The drop-down menu at the top right -----
function toggleUserMenu(event) {
    event.stopPropagation();
    document.getElementById('userDropdown').classList.toggle('open');
}
document.addEventListener('click', function () {
    document.getElementById('userDropdown').classList.remove('open');
});

// ----- POPUP 2 : edit quiz -----
function openEdit(id) {
    var row = rowOf(id);
    document.getElementById('hfEditId').value = id;
    document.getElementById('txtEditTitle').value = row.getAttribute('data-title');
    document.getElementById('ddlEditLesson').value = row.getAttribute('data-lessonid');
    document.getElementById('txtEditPass').value = row.getAttribute('data-pass');
    buildQuestions(id, 'eQuestions', 'eQCount', 'edit');   // the questions of this quiz (add / edit / delete)
    openModal('editModal');
}

// ----- POPUP 4 : delete quiz -----
function openDelete(id) {
    document.getElementById('hfDeleteId').value = id;
    show('delName', rowOf(id).getAttribute('data-title'));
    openModal('deleteModal');
}

// ----- POPUP 5 : add / edit ONE question  (qid = 0 means a new question) -----
function openQuestion(qid, quizId, from) {
    closeModal('detailModal');
    closeModal('editModal');

    document.getElementById('hfQId').value = qid;
    document.getElementById('hfQQuiz').value = quizId;
    document.getElementById('hfQFrom').value = from || 'detail';

    if (qid == 0) {
        show('qTitle', 'Add Question');
        document.getElementById('txtQText').value = '';
        document.getElementById('txtQA').value = '';
        document.getElementById('txtQB').value = '';
        document.getElementById('txtQC').value = '';
        document.getElementById('txtQD').value = '';
        document.getElementById('ddlQCorrect').value = 'A';
        document.getElementById('txtQExplain').value = '';
        setDifficulty('Medium');
        document.getElementById('txtQPoints').value = '10';
    } else {
        var q = document.getElementById('qd' + qid);
        show('qTitle', 'Edit Question');
        document.getElementById('txtQText').value = q.getAttribute('data-text');
        document.getElementById('txtQA').value = q.getAttribute('data-a');
        document.getElementById('txtQB').value = q.getAttribute('data-b');
        document.getElementById('txtQC').value = q.getAttribute('data-c');
        document.getElementById('txtQD').value = q.getAttribute('data-d');
        document.getElementById('ddlQCorrect').value = q.getAttribute('data-correct') || 'A';
        document.getElementById('txtQExplain').value = q.getAttribute('data-explain');
        setDifficulty(q.getAttribute('data-difficulty'));
        document.getElementById('txtQPoints').value = q.getAttribute('data-points') || '10';
    }
    openModal('questionModal');
}

// Chooses the difficulty in the dropdown (it does not matter if it is written easy, Easy or EASY)
function setDifficulty(value) {
    var box = document.getElementById('ddlQDifficulty');
    box.value = 'Medium';
    for (var i = 0; i < box.options.length; i++) {
        if (box.options[i].value.toLowerCase() == String(value).toLowerCase()) { box.value = box.options[i].value; }
    }
}

// Cancel in the question popup: go back to the quiz popup
function cancelQuestion() {
    closeModal('questionModal');
    goBack(document.getElementById('hfQFrom').value, document.getElementById('hfQQuiz').value);
}

// ----- POPUP 6 : delete ONE question -----
function openQuestionDelete(qid, quizId, from) {
    closeModal('detailModal');
    closeModal('editModal');
    document.getElementById('hfQDeleteFrom').value = from || 'detail';
    document.getElementById('hfQDeleteId').value = qid;
    document.getElementById('hfQDeleteQuiz').value = quizId;
    show('delQText', document.getElementById('qd' + qid).getAttribute('data-text'));
    openModal('questionDeleteModal');
}

function cancelQuestionDelete() {
    closeModal('questionDeleteModal');
    goBack(document.getElementById('hfQDeleteFrom').value, document.getElementById('hfQDeleteQuiz').value);
}
    </script>
</head>

<body>
    <form id="form1" runat="server">


        <!-- ==================================================
             PART 1 : SIDEBAR (dark menu on the left)
             ================================================== -->
        <div class="sidebar">
            <img src="../../images/logo.png" alt="EduCore" class="logo" />

            <a class="menu-item" href="adminDashboard.aspx">
                <svg class="menu-icon" viewBox="0 0 24 24"><rect x="3" y="3" width="7" height="7" /><rect x="14" y="3" width="7" height="7" /><rect x="14" y="14" width="7" height="7" /><rect x="3" y="14" width="7" height="7" /></svg>
                Dashboard
            </a>
            <a class="menu-item" href="userManagement.aspx">
                <svg class="menu-icon" viewBox="0 0 24 24"><path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="M23 21v-2a4 4 0 00-3-3.87" /><path d="M16 3.13a4 4 0 010 7.75" /></svg>
                Users
            </a>
            <a class="menu-item" href="coursesManagement.aspx">
                <svg class="menu-icon" viewBox="0 0 24 24"><path d="M2 3h6a4 4 0 014 4v14a3 3 0 00-3-3H2z" /><path d="M22 3h-6a4 4 0 00-4 4v14a3 3 0 013-3h7z" /></svg>
                Courses
            </a>
            <a class="menu-item" href="lessonManagement.aspx">
                <svg class="menu-icon" viewBox="0 0 24 24"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z" /><polyline points="14 2 14 8 20 8" /><line x1="16" y1="13" x2="8" y2="13" /><line x1="16" y1="17" x2="8" y2="17" /></svg>
                Lessons
            </a>
            <a class="menu-item active" href="quizManagement.aspx">
                <svg class="menu-icon" viewBox="0 0 24 24"><circle cx="12" cy="12" r="10" /><path d="M9.09 9a3 3 0 015.83 1c0 2-3 3-3 3" /><line x1="12" y1="17" x2="12.01" y2="17" /></svg>
                Quizzes
            </a>
            <a class="menu-item" href="adminReports.aspx">
                <svg class="menu-icon" viewBox="0 0 24 24"><line x1="18" y1="20" x2="18" y2="10" /><line x1="12" y1="20" x2="12" y2="4" /><line x1="6" y1="20" x2="6" y2="14" /></svg>
                Reports
            </a>

            <div class="me">
                <%= AdminName %>
                <small>Platform administrator</small>
            </div>
        </div>


        <!-- ==================================================
             PART 2 : EVERYTHING ON THE RIGHT SIDE
             ================================================== -->
        <div class="main">

            <!-- ---------- Top bar ---------- -->
            <div class="topbar">
                <div>
                    <h1>Quiz Management</h1>
                    <p>Create quizzes and keep their questions up to date</p>
                </div>
                <div class="user-menu">
                    <button type="button" class="user-btn" onclick="toggleUserMenu(event)" title="Account">
                        <span class="avatar"><%= AdminInitials %></span>
                    </button>

                    <div class="user-dropdown" id="userDropdown">
                        <a href="adminSettings.aspx">Manage Account</a>
                        <asp:LinkButton ID="lnkLogout" runat="server" CssClass="logout" CausesValidation="false" OnClick="lnkLogout_Click">Logout</asp:LinkButton>
                    </div>
                </div>
            </div>

            <div class="content">

                <!-- Green / red message after saving -->
                <asp:Literal ID="litMessage" runat="server"></asp:Literal>


                <!-- ==============================================
                     5 NUMBER BOXES
                     ============================================== -->
                <div class="stat-row five">

                    <div class="card">
                        <div class="stat-top">
                            <div class="stat-label">All quizzes</div>
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10" /><path d="M9.09 9a3 3 0 015.83 1c0 2-3 3-3 3" /><line x1="12" y1="17" x2="12.01" y2="17" /></svg>
                        </div>
                        <div class="stat-number"><asp:Label ID="lblTotal" runat="server"></asp:Label></div>
                        <div class="stat-note"><asp:Label ID="lblTotalNote" runat="server"></asp:Label></div>
                    </div>

                    <div class="card">
                        <div class="stat-top">
                            <div class="stat-label">Questions</div>
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="8" y1="6" x2="21" y2="6" /><line x1="8" y1="12" x2="21" y2="12" /><line x1="8" y1="18" x2="21" y2="18" /><line x1="3" y1="6" x2="3.01" y2="6" /><line x1="3" y1="12" x2="3.01" y2="12" /><line x1="3" y1="18" x2="3.01" y2="18" /></svg>
                        </div>
                        <div class="stat-number"><asp:Label ID="lblQuestions" runat="server"></asp:Label></div>
                        <div class="stat-note"><asp:Label ID="lblQuestionsNote" runat="server"></asp:Label></div>
                    </div>

                    <div class="card">
                        <div class="stat-top">
                            <div class="stat-label">Lessons with quiz</div>
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z" /><polyline points="14 2 14 8 20 8" /></svg>
                        </div>
                        <div class="stat-number"><asp:Label ID="lblLessons" runat="server"></asp:Label></div>
                        <div class="stat-note"><asp:Label ID="lblLessonsNote" runat="server"></asp:Label></div>
                    </div>

                    <div class="card">
                        <div class="stat-top">
                            <div class="stat-label">Passing score</div>
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="8" r="6" /><path d="M15.5 13.5L17 22l-5-3-5 3 1.5-8.5" /></svg>
                        </div>
                        <div class="stat-number"><asp:Label ID="lblPass" runat="server"></asp:Label></div>
                        <div class="stat-note">Average of all quizzes</div>
                    </div>

                    <div class="card">
                        <div class="stat-top">
                            <div class="stat-label">Attempts</div>
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12" /></svg>
                        </div>
                        <div class="stat-number"><asp:Label ID="lblAttempts" runat="server"></asp:Label></div>
                        <div class="stat-note">Quiz scores saved</div>
                    </div>

                </div>


                <!-- ==============================================
                     QUIZ TABLE CARD
                     ============================================== -->
                <div class="card flush">

                    <asp:Panel ID="pnlToolbar" runat="server" CssClass="toolbar" DefaultButton="btnSearch">

                        <asp:TextBox ID="txtSearch" runat="server" CssClass="input search-box" placeholder="Search quiz, lesson or course"></asp:TextBox>

                        <asp:DropDownList ID="ddlCourse" runat="server" CssClass="input" AutoPostBack="true" OnSelectedIndexChanged="Filter_Changed">
                        </asp:DropDownList>

                        <asp:DropDownList ID="ddlQuestionFilter" runat="server" CssClass="input" AutoPostBack="true" OnSelectedIndexChanged="Filter_Changed">
                            <asp:ListItem Text="All quizzes" Value=""></asp:ListItem>
                            <asp:ListItem Text="With questions" Value="with"></asp:ListItem>
                            <asp:ListItem Text="Without questions" Value="without"></asp:ListItem>
                        </asp:DropDownList>

                        <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn" OnClick="Filter_Changed" />

                        <div class="spacer"></div>

                        <button type="button" class="btn btn-primary" onclick="openModal('addModal')">+ Create Quiz</button>

                    </asp:Panel>


                    <asp:Panel ID="pnlTable" runat="server">

                        <table class="data">
                            <tr>
                                <th class="c-id">ID</th>
                                <th class="c-quiz">Quiz</th>
                                <th class="c-lesson">Lesson</th>
                                <th class="c-course">Course</th>
                                <th class="c-q center">Questions</th>
                                <th class="c-pass center">Pass</th>
                                <th class="c-att center">Attempts</th>
                                <th class="c-actions">Actions</th>
                            </tr>

                            <!-- C# fills the rows here -->
                            <asp:Literal ID="litRows" runat="server" EnableViewState="false"></asp:Literal>
                        </table>

                        <div class="table-footer">
                            <asp:Label ID="lblShowing" runat="server"></asp:Label>
                            <div>
                                <asp:Button ID="btnPrev" runat="server" Text="Previous" CssClass="btn" OnClick="btnPrev_Click" />
                                <asp:Button ID="btnNext" runat="server" Text="Next" CssClass="btn" OnClick="btnNext_Click" />
                            </div>
                        </div>

                    </asp:Panel>


                    <asp:Panel ID="pnlEmpty" runat="server" Visible="false">
                        <div class="empty-state">
                            <div class="empty-icon">
                                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round">
                                    <circle cx="11" cy="11" r="7" /><line x1="21" y1="21" x2="16.5" y2="16.5" />
                                </svg>
                            </div>
                            <div class="empty-title">No results found</div>
                            Try another keyword, clear the search, or change the filters to see all quizzes.
                        </div>
                    </asp:Panel>

                </div>

            </div>
        </div>


        <!-- The questions of the quizzes on this page. They are hidden: the quiz popup reads them. -->
        <div style="display:none;">
            <asp:Literal ID="litQuestions" runat="server" EnableViewState="false"></asp:Literal>
        </div>


        <!-- ==================================================
             POPUP 1 : QUIZ DETAIL + QUESTIONS (opens when you click a row)
             ================================================== -->
        <div class="modal-back" id="detailModal">
            <div class="modal-box wide">

                <div class="modal-head">
                    <h3>Quiz detail</h3>
                </div>

                <div class="modal-title" id="dTitle"></div>
                <p class="modal-text">The quiz settings and every question inside it.</p>

                <div class="info-box">
                    <div class="info-label">Lesson</div>
                    <div class="info-value" id="dLesson"></div>

                    <div class="info-label">Course</div>
                    <div class="info-value" id="dCourse"></div>

                    <div class="info-label">Passing score</div>
                    <div class="info-value" id="dPass"></div>

                    <div class="info-label">Attempts so far</div>
                    <div class="info-value" id="dAttempts"></div>

                    <div class="info-label">Quiz ID</div>
                    <div class="info-value" id="dId"></div>
                </div>

                <div class="q-head">
                    <h4>Questions (<span id="dQCount"></span>)</h4>
                    <button type="button" class="btn btn-primary" style="height:32px;" onclick="addQuestionFromDetail()">+ Add Question</button>
                </div>

                <!-- JavaScript draws the question cards here -->
                <div id="dQuestions"></div>

                <div class="modal-buttons">
                    <button type="button" class="btn" onclick="editFromDetail()">Edit Quiz</button>
                    <button type="button" class="btn" onclick="closeModal('detailModal')">Close</button>
                </div>

            </div>
        </div>


        <!-- ==================================================
             POPUP 2 : EDIT QUIZ
             ================================================== -->
        <div class="modal-back" id="editModal">
            <div class="modal-box wide">

                <div class="modal-head">
                    <h3>Edit Quiz</h3>
                </div>

                <asp:HiddenField ID="hfEditId" runat="server" ClientIDMode="Static" />

                <div class="form-field">
                    <label>Quiz title</label>
                    <asp:TextBox ID="txtEditTitle" runat="server" ClientIDMode="Static" CssClass="input"></asp:TextBox>
                </div>

                <div class="form-field">
                    <label>Lesson (a lesson can only have one quiz)</label>
                    <asp:DropDownList ID="ddlEditLesson" runat="server" ClientIDMode="Static" CssClass="input"></asp:DropDownList>
                </div>

                <div class="form-field">
                    <label>Passing score (0 - 100)</label>
                    <asp:TextBox ID="txtEditPass" runat="server" ClientIDMode="Static" CssClass="input" MaxLength="3"></asp:TextBox>
                </div>

                <!-- The questions of this quiz: add, edit and delete them here -->
                <div class="q-head">
                    <h4>Questions (<span id="eQCount"></span>)</h4>
                    <button type="button" class="btn btn-primary" style="height:32px;" onclick="addQuestionFromEdit()">+ Add Question</button>
                </div>
                <div id="eQuestions"></div>

                <div class="modal-buttons">
                    <button type="button" class="btn" onclick="closeModal('editModal')">Cancel</button>
                    <asp:Button ID="btnSaveEdit" runat="server" Text="Save changes" CssClass="btn btn-primary" OnClick="btnSaveEdit_Click" />
                </div>

            </div>
        </div>


        <!-- ==================================================
             POPUP 3 : CREATE QUIZ
             ================================================== -->
        <div class="modal-back" id="addModal">
            <div class="modal-box">

                <div class="modal-head">
                    <h3>Create Quiz</h3>
                    <span class="badge blue">New</span>
                </div>

                <div class="form-field">
                    <label>Quiz title</label>
                    <asp:TextBox ID="txtAddTitle" runat="server" CssClass="input" placeholder="e.g. Cell Structure Checkpoint"></asp:TextBox>
                </div>

                <div class="form-field">
                    <label>Lesson (only lessons without a quiz are listed)</label>
                    <asp:DropDownList ID="ddlAddLesson" runat="server" CssClass="input"></asp:DropDownList>
                </div>

                <div class="form-field">
                    <label>Passing score (0 - 100)</label>
                    <asp:TextBox ID="txtAddPass" runat="server" CssClass="input" MaxLength="3" Text="50"></asp:TextBox>
                </div>

                <div class="form-error">
                    <asp:Label ID="lblAddError" runat="server"></asp:Label>
                </div>

                <div class="modal-buttons">
                    <button type="button" class="btn" onclick="closeModal('addModal')">Cancel</button>
                    <asp:Button ID="btnAddQuiz" runat="server" Text="Create Quiz" CssClass="btn btn-primary" OnClick="btnAddQuiz_Click" />
                </div>

            </div>
        </div>


        <!-- ==================================================
             POPUP 4 : DELETE QUIZ
             ================================================== -->
        <div class="modal-back" id="deleteModal">
            <div class="modal-box danger">

                <div class="modal-head">
                    <h3>Delete Quiz</h3>
                </div>

                <asp:HiddenField ID="hfDeleteId" runat="server" ClientIDMode="Static" />

                <div class="modal-title">Are you sure you want to delete <span id="delName"></span>?</div>
                <p class="modal-text">This quiz will be permanently removed, together with all its questions and the scores students got. The lesson itself stays. This cannot be undone.</p>

                <div class="modal-buttons">
                    <button type="button" class="btn" onclick="closeModal('deleteModal')">Cancel</button>
                    <asp:Button ID="btnConfirmDelete" runat="server" Text="Confirm" CssClass="btn btn-danger" OnClick="btnConfirmDelete_Click" />
                </div>

            </div>
        </div>


        <!-- ==================================================
             POPUP 5 : ADD / EDIT ONE QUESTION
             ================================================== -->
        <div class="modal-back" id="questionModal">
            <div class="modal-box">

                <div class="modal-head">
                    <h3 id="qTitle">Add Question</h3>
                </div>

                <asp:HiddenField ID="hfQId" runat="server" ClientIDMode="Static" />
                <asp:HiddenField ID="hfQQuiz" runat="server" ClientIDMode="Static" />
                <asp:HiddenField ID="hfQFrom" runat="server" ClientIDMode="Static" />

                <div class="form-field">
                    <label>Question</label>
                    <asp:TextBox ID="txtQText" runat="server" ClientIDMode="Static" TextMode="MultiLine" Rows="3" CssClass="input textarea"></asp:TextBox>
                </div>

                <div class="form-field">
                    <label>Answer A</label>
                    <asp:TextBox ID="txtQA" runat="server" ClientIDMode="Static" CssClass="input"></asp:TextBox>
                </div>
                <div class="form-field">
                    <label>Answer B</label>
                    <asp:TextBox ID="txtQB" runat="server" ClientIDMode="Static" CssClass="input"></asp:TextBox>
                </div>
                <div class="form-field">
                    <label>Answer C (optional)</label>
                    <asp:TextBox ID="txtQC" runat="server" ClientIDMode="Static" CssClass="input"></asp:TextBox>
                </div>
                <div class="form-field">
                    <label>Answer D (optional)</label>
                    <asp:TextBox ID="txtQD" runat="server" ClientIDMode="Static" CssClass="input"></asp:TextBox>
                </div>

                <div class="form-field">
                    <label>Correct answer</label>
                    <asp:DropDownList ID="ddlQCorrect" runat="server" ClientIDMode="Static" CssClass="input">
                        <asp:ListItem Text="A" Value="A"></asp:ListItem>
                        <asp:ListItem Text="B" Value="B"></asp:ListItem>
                        <asp:ListItem Text="C" Value="C"></asp:ListItem>
                        <asp:ListItem Text="D" Value="D"></asp:ListItem>
                    </asp:DropDownList>
                </div>

                <div class="form-row">
                    <div class="form-field">
                        <label>Difficulty</label>
                        <asp:DropDownList ID="ddlQDifficulty" runat="server" ClientIDMode="Static" CssClass="input">
                            <asp:ListItem Text="Easy" Value="Easy"></asp:ListItem>
                            <asp:ListItem Text="Medium" Value="Medium" Selected="True"></asp:ListItem>
                            <asp:ListItem Text="Hard" Value="Hard"></asp:ListItem>
                        </asp:DropDownList>
                    </div>
                    <div class="form-field">
                        <label>Points</label>
                        <asp:TextBox ID="txtQPoints" runat="server" ClientIDMode="Static" CssClass="input" MaxLength="4" Text="10"></asp:TextBox>
                    </div>
                </div>

                <div class="form-field">
                    <label>Explanation (optional)</label>
                    <asp:TextBox ID="txtQExplain" runat="server" ClientIDMode="Static" TextMode="MultiLine" Rows="2" CssClass="input textarea"></asp:TextBox>
                </div>

                <div class="modal-buttons">
                    <button type="button" class="btn" onclick="cancelQuestion()">Cancel</button>
                    <asp:Button ID="btnSaveQuestion" runat="server" Text="Save question" CssClass="btn btn-primary" OnClick="btnSaveQuestion_Click" />
                </div>

            </div>
        </div>


        <!-- ==================================================
             POPUP 6 : DELETE ONE QUESTION
             ================================================== -->
        <div class="modal-back" id="questionDeleteModal">
            <div class="modal-box danger">

                <div class="modal-head">
                    <h3>Delete Question</h3>
                </div>

                <asp:HiddenField ID="hfQDeleteId" runat="server" ClientIDMode="Static" />
                <asp:HiddenField ID="hfQDeleteQuiz" runat="server" ClientIDMode="Static" />
                <asp:HiddenField ID="hfQDeleteFrom" runat="server" ClientIDMode="Static" />

                <div class="modal-title">Delete this question?</div>
                <p class="modal-text"><span id="delQText"></span></p>

                <div class="modal-buttons">
                    <button type="button" class="btn" onclick="cancelQuestionDelete()">Cancel</button>
                    <asp:Button ID="btnConfirmQuestionDelete" runat="server" Text="Confirm" CssClass="btn btn-danger" OnClick="btnConfirmQuestionDelete_Click" />
                </div>

            </div>
        </div>

    </form>
</body>
</html>
