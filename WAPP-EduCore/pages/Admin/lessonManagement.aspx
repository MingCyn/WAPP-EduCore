<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="lessonManagement.aspx.cs" Inherits="WAPP_EduCore.pages.Admin.lessonManagement" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">

<head runat="server">
    <title>EduCore - Lesson Management</title>

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

        /* Big text box */
        .input.textarea { height: auto; padding: 10px 12px; font-family: inherit; resize: vertical; }

        /* Small grey line under a box */
        .form-hint { margin: -6px 0 14px 0; color: var(--grey); font-size: 12px; }

        /* The "which course" text under a lesson title in the table */
        .sub-line { margin-top: 2px; color: var(--grey); font-size: 12px; }

        /* A blue badge */
        .badge.blue { background: #EEF2FF; color: var(--blue); }

        /* The lesson text in the detail popup */
        .text-box { margin-bottom: 16px; padding: 12px 14px; background: white; border: 1px solid var(--line); border-radius: 10px; color: var(--text); font-size: 13px; line-height: 1.5; white-space: pre-wrap; }

        /* ---------- Table columns: fixed widths, so the 3 action icons stay on ONE row ---------- */
        table.data { table-layout: fixed; }
        th.c-id      { width: 56px; }
        th.c-lesson  { width: 30%; }      /* smaller = the Lesson column is narrower */
        th.c-course  { width: 26%; }
        th.c-video   { width: 100px; }
        th.c-quiz    { width: 130px; }
        th.c-created { width: 120px; }
        th.c-actions { width: 150px; }    /* bigger = more room for the icons */
        td.actions, td.nowrap { white-space: nowrap; }
        .badge { white-space: nowrap; }
    </style>

    <!-- ==================================================
         JAVASCRIPT : controls the popups
         ================================================== -->
    <script type="text/javascript">

var currentId = 0;   // the lesson we clicked on

// ----- Show / hide a popup -----
function openModal(id) { document.getElementById(id).classList.add('open'); }
function closeModal(id) { document.getElementById(id).classList.remove('open'); }

// ----- Helpers -----
function rowOf(id) { return document.getElementById('row' + id); }
function show(id, text) { document.getElementById(id).innerText = text; }
function orDash(text) { return (text == null || text == '') ? '—' : text; }

// ----- POPUP 1 : lesson detail (when you click a row) -----
function openDetail(id) {
    currentId = id;
    var row = rowOf(id);

    show('dTitle', row.getAttribute('data-title'));
    show('dCourse', orDash(row.getAttribute('data-course')));
    show('dCreated', row.getAttribute('data-created'));
    show('dId', id);

    // description + lesson text (hidden when empty)
    var desc = row.getAttribute('data-description');
    show('dDesc', desc);
    document.getElementById('dDesc').style.display = (desc == '') ? 'none' : 'block';

    var text = row.getAttribute('data-text');
    show('dText', text);
    document.getElementById('dTextWrap').style.display = (text == '') ? 'none' : 'block';

    // the video link (clickable)
    var video = row.getAttribute('data-video');
    var link = document.getElementById('dVideo');
    if (video == '') { link.style.display = 'none'; show('dVideoNone', '—'); }
    else { link.style.display = 'inline'; link.href = video; link.innerText = video; show('dVideoNone', ''); }

    // the quiz
    var quizId = row.getAttribute('data-quizid');
    if (quizId == '') { show('dQuiz', 'No quiz yet'); }
    else { show('dQuiz', row.getAttribute('data-quiz') + '  (' + row.getAttribute('data-qcount') + ' questions)'); }

    openModal('detailModal');
}

// The "Edit Lesson" button inside the detail popup
function editFromDetail() {
    closeModal('detailModal');
    openEdit(currentId);
}

// ----- The drop-down menu at the top right -----
function toggleUserMenu(event) {
    event.stopPropagation();
    document.getElementById('userDropdown').classList.toggle('open');
}
document.addEventListener('click', function () {
    document.getElementById('userDropdown').classList.remove('open');
});

// ----- POPUP 2 : edit lesson -----
function openEdit(id) {
    var row = rowOf(id);
    document.getElementById('hfEditId').value = id;
    document.getElementById('ddlEditCourse').value = row.getAttribute('data-courseid');
    document.getElementById('txtEditTitle').value = row.getAttribute('data-title');
    document.getElementById('txtEditDescription').value = row.getAttribute('data-description');
    document.getElementById('txtEditVideo').value = row.getAttribute('data-video');
    document.getElementById('txtEditText').value = row.getAttribute('data-text');
    openModal('editModal');
}

// ----- POPUP 4 : delete lesson (asks "are you sure?") -----
function openDelete(id) {
    document.getElementById('hfDeleteId').value = id;
    show('delName', rowOf(id).getAttribute('data-title'));
    openModal('deleteModal');
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
            <a class="menu-item active" href="lessonManagement.aspx">
                <svg class="menu-icon" viewBox="0 0 24 24"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z" /><polyline points="14 2 14 8 20 8" /><line x1="16" y1="13" x2="8" y2="13" /><line x1="16" y1="17" x2="8" y2="17" /></svg>
                Lessons
            </a>
            <a class="menu-item" href="quizManagement.aspx">
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
                    <h1>Lesson Management</h1>
                    <p>Create, review and maintain the lessons of every course</p>
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
                            <div class="stat-label">All lessons</div>
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z" /><polyline points="14 2 14 8 20 8" /></svg>
                        </div>
                        <div class="stat-number"><asp:Label ID="lblTotal" runat="server"></asp:Label></div>
                        <div class="stat-note">Saved in the database</div>
                    </div>

                    <div class="card">
                        <div class="stat-top">
                            <div class="stat-label">Courses covered</div>
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M2 3h6a4 4 0 014 4v14a3 3 0 00-3-3H2z" /><path d="M22 3h-6a4 4 0 00-4 4v14a3 3 0 013-3h7z" /></svg>
                        </div>
                        <div class="stat-number"><asp:Label ID="lblCourses" runat="server"></asp:Label></div>
                        <div class="stat-note"><asp:Label ID="lblCoursesNote" runat="server"></asp:Label></div>
                    </div>

                    <div class="card">
                        <div class="stat-top">
                            <div class="stat-label">With video</div>
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="5" width="14" height="14" rx="2" /><path d="M16 10l6-3v10l-6-3z" /></svg>
                        </div>
                        <div class="stat-number"><asp:Label ID="lblVideo" runat="server"></asp:Label></div>
                        <div class="stat-note"><asp:Label ID="lblVideoNote" runat="server"></asp:Label></div>
                    </div>

                    <div class="card">
                        <div class="stat-top">
                            <div class="stat-label">With quiz</div>
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10" /><path d="M9.09 9a3 3 0 015.83 1c0 2-3 3-3 3" /><line x1="12" y1="17" x2="12.01" y2="17" /></svg>
                        </div>
                        <div class="stat-number"><asp:Label ID="lblQuiz" runat="server"></asp:Label></div>
                        <div class="stat-note"><asp:Label ID="lblQuizNote" runat="server"></asp:Label></div>
                    </div>

                    <div class="card">
                        <div class="stat-top">
                            <div class="stat-label">Lessons per course</div>
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="20" x2="18" y2="10" /><line x1="12" y1="20" x2="12" y2="4" /><line x1="6" y1="20" x2="6" y2="14" /></svg>
                        </div>
                        <div class="stat-number"><asp:Label ID="lblAverage" runat="server"></asp:Label></div>
                        <div class="stat-note">Average</div>
                    </div>

                </div>


                <!-- ==============================================
                     LESSON TABLE CARD
                     ============================================== -->
                <div class="card flush">

                    <!-- Search + filters + Create Lesson button.
                         DefaultButton = pressing ENTER in the box clicks "Search" -->
                    <asp:Panel ID="pnlToolbar" runat="server" CssClass="toolbar" DefaultButton="btnSearch">

                        <asp:TextBox ID="txtSearch" runat="server" CssClass="input search-box" placeholder="Search lesson or course"></asp:TextBox>

                        <!-- C# fills this dropdown with the courses -->
                        <asp:DropDownList ID="ddlCourse" runat="server" CssClass="input" AutoPostBack="true" OnSelectedIndexChanged="Filter_Changed">
                        </asp:DropDownList>

                        <asp:DropDownList ID="ddlQuizFilter" runat="server" CssClass="input" AutoPostBack="true" OnSelectedIndexChanged="Filter_Changed">
                            <asp:ListItem Text="All lessons" Value=""></asp:ListItem>
                            <asp:ListItem Text="With quiz" Value="with"></asp:ListItem>
                            <asp:ListItem Text="Without quiz" Value="without"></asp:ListItem>
                        </asp:DropDownList>

                        <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn" OnClick="Filter_Changed" />

                        <div class="spacer"></div>

                        <button type="button" class="btn btn-primary" onclick="openModal('addModal')">+ Create Lesson</button>

                    </asp:Panel>


                    <!-- The table (only shown when we found lessons) -->
                    <asp:Panel ID="pnlTable" runat="server">

                        <table class="data">
                            <tr>
                                <th class="c-id">ID</th>
                                <th class="c-lesson">Lesson</th>
                                <th class="c-course">Course</th>
                                <th class="c-video">Video</th>
                                <th class="c-quiz">Quiz</th>
                                <th class="c-created">Created</th>
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


                    <!-- "No results found" -->
                    <asp:Panel ID="pnlEmpty" runat="server" Visible="false">
                        <div class="empty-state">
                            <div class="empty-icon">
                                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round">
                                    <circle cx="11" cy="11" r="7" /><line x1="21" y1="21" x2="16.5" y2="16.5" />
                                </svg>
                            </div>
                            <div class="empty-title">No results found</div>
                            Try another keyword, clear the search, or change the filters to see all lessons.
                        </div>
                    </asp:Panel>

                </div>

            </div>
        </div>


        <!-- ==================================================
             POPUP 1 : LESSON DETAIL  (opens when you click a row)
             ================================================== -->
        <div class="modal-back" id="detailModal">
            <div class="modal-box">

                <div class="modal-head">
                    <h3>Lesson detail</h3>
                </div>

                <div class="modal-title" id="dTitle"></div>
                <p class="modal-text" id="dDesc"></p>

                <div id="dTextWrap">
                    <div class="info-label" style="margin-bottom:6px;">Lesson text</div>
                    <div class="text-box" id="dText"></div>
                </div>

                <div class="info-box">
                    <div class="info-label">Course</div>
                    <div class="info-value" id="dCourse"></div>

                    <div class="info-label">Video link</div>
                    <div class="info-value"><a id="dVideo" href="#" target="_blank" style="color:var(--blue);"></a><span id="dVideoNone"></span></div>

                    <div class="info-label">Quiz</div>
                    <div class="info-value" id="dQuiz"></div>

                    <div class="info-label">Created</div>
                    <div class="info-value" id="dCreated"></div>

                    <div class="info-label">Lesson ID</div>
                    <div class="info-value" id="dId"></div>
                </div>

                <div class="modal-buttons">
                    <button type="button" class="btn btn-primary" onclick="editFromDetail()">Edit Lesson</button>
                    <button type="button" class="btn" onclick="closeModal('detailModal')">Close</button>
                </div>

            </div>
        </div>


        <!-- ==================================================
             POPUP 2 : EDIT LESSON
             ================================================== -->
        <div class="modal-back" id="editModal">
            <div class="modal-box">

                <div class="modal-head">
                    <h3>Edit Lesson</h3>
                </div>

                <asp:HiddenField ID="hfEditId" runat="server" ClientIDMode="Static" />

                <div class="form-field">
                    <label>Course</label>
                    <asp:DropDownList ID="ddlEditCourse" runat="server" ClientIDMode="Static" CssClass="input"></asp:DropDownList>
                </div>

                <div class="form-field">
                    <label>Lesson title</label>
                    <asp:TextBox ID="txtEditTitle" runat="server" ClientIDMode="Static" CssClass="input"></asp:TextBox>
                </div>

                <div class="form-field">
                    <label>Lesson summary</label>
                    <asp:TextBox ID="txtEditDescription" runat="server" ClientIDMode="Static" TextMode="MultiLine" Rows="3" CssClass="input textarea"></asp:TextBox>
                </div>

                <div class="form-field">
                    <label>Video link (https://...)</label>
                    <asp:TextBox ID="txtEditVideo" runat="server" ClientIDMode="Static" CssClass="input" placeholder="https://www.youtube.com/watch?v=..."></asp:TextBox>
                </div>

                <div class="form-field">
                    <label>Lesson text (paragraph)</label>
                    <asp:TextBox ID="txtEditText" runat="server" ClientIDMode="Static" TextMode="MultiLine" Rows="4" CssClass="input textarea"></asp:TextBox>
                </div>

                <div class="modal-buttons">
                    <button type="button" class="btn" onclick="closeModal('editModal')">Cancel</button>
                    <asp:Button ID="btnSaveEdit" runat="server" Text="Save changes" CssClass="btn btn-primary" OnClick="btnSaveEdit_Click" />
                </div>

            </div>
        </div>


        <!-- ==================================================
             POPUP 3 : CREATE LESSON
             ================================================== -->
        <div class="modal-back" id="addModal">
            <div class="modal-box">

                <div class="modal-head">
                    <h3>Create Lesson</h3>
                    <span class="badge blue">New</span>
                </div>

                <div class="form-field">
                    <label>Course</label>
                    <asp:DropDownList ID="ddlAddCourse" runat="server" CssClass="input"></asp:DropDownList>
                </div>

                <div class="form-field">
                    <label>Lesson title</label>
                    <asp:TextBox ID="txtAddTitle" runat="server" CssClass="input" placeholder="e.g. Lesson 1: Cell Structure and Function"></asp:TextBox>
                </div>

                <div class="form-field">
                    <label>Lesson summary</label>
                    <asp:TextBox ID="txtAddDescription" runat="server" TextMode="MultiLine" Rows="3" CssClass="input textarea" placeholder="A short overview students see before the lesson starts"></asp:TextBox>
                </div>

                <div class="form-field">
                    <label>Video link (https://...)</label>
                    <asp:TextBox ID="txtAddVideo" runat="server" CssClass="input" placeholder="https://www.youtube.com/watch?v=..."></asp:TextBox>
                </div>

                <div class="form-field">
                    <label>Lesson text (paragraph)</label>
                    <asp:TextBox ID="txtAddText" runat="server" TextMode="MultiLine" Rows="4" CssClass="input textarea" placeholder="Write the explanation students will read"></asp:TextBox>
                </div>

                <div class="form-error">
                    <asp:Label ID="lblAddError" runat="server"></asp:Label>
                </div>

                <div class="modal-buttons">
                    <button type="button" class="btn" onclick="closeModal('addModal')">Cancel</button>
                    <asp:Button ID="btnAddLesson" runat="server" Text="Create Lesson" CssClass="btn btn-primary" OnClick="btnAddLesson_Click" />
                </div>

            </div>
        </div>


        <!-- ==================================================
             POPUP 4 : DELETE LESSON (asks "are you sure?")
             ================================================== -->
        <div class="modal-back" id="deleteModal">
            <div class="modal-box danger">

                <div class="modal-head">
                    <h3>Delete Lesson</h3>
                </div>

                <asp:HiddenField ID="hfDeleteId" runat="server" ClientIDMode="Static" />

                <div class="modal-title">Are you sure you want to delete <span id="delName"></span>?</div>
                <p class="modal-text">This lesson will be permanently removed from the database, together with its quiz, quiz questions, quiz scores, student progress and discussions. This cannot be undone.</p>

                <div class="modal-buttons">
                    <button type="button" class="btn" onclick="closeModal('deleteModal')">Cancel</button>
                    <asp:Button ID="btnConfirmDelete" runat="server" Text="Confirm" CssClass="btn btn-danger" OnClick="btnConfirmDelete_Click" />
                </div>

            </div>
        </div>

    </form>
</body>
</html>
