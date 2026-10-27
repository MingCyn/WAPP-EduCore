<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="coursesManagement.aspx.cs" Inherits="WAPP_EduCore.pages.Admin.WebForm4" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">

<head runat="server">
    <title>EduCore - Course Management</title>

    <!-- Font -->
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet" />

    <!-- Our own styles (colors, cards, tables, popups...) -->
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />

    <!-- Small extra styles only this page needs (so admin.css does not change) -->
    <style type="text/css">

        /* The little arrow of every dropdown, moved a bit to the left */
        select.input {
            appearance: none;
            -webkit-appearance: none;
            padding-right: 40px;
            background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='%2364748B' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpolyline points='6 9 12 15 18 9'/%3E%3C/svg%3E");
            background-repeat: no-repeat;
            background-position: right 16px center;
            background-size: 14px;
        }

        /* Green "Published" and red "Draft" badges (the Status column) */
        .badge.published {
            background: #DCFCE7;
            color: #15803D;
        }

        .badge.draft {
            background: #FEE2E2;
            color: #B91C1C;
        }

        /* A blue "New" badge */
        .badge.new {
            background: #EEF2FF;
            color: var(--blue);
        }

        /* ---------- The course preview card (used in the Create + Detail popups) ---------- */
        .preview-card {
            margin-bottom: 16px;
            padding: 14px;
            background: #F6F7FB;
            border: 1px solid var(--line);
            border-radius: 12px;
        }

        /* The picture box on top of the card */
        .preview-img-box {
            display: flex;
            align-items: center;
            justify-content: center;
            height: 150px;
            margin-bottom: 14px;
            overflow: hidden;
            background: linear-gradient(135deg, #DCE6F7, #C9D8F2);
            border-radius: 10px;
            color: var(--grey);
            font-size: 12px;
        }

        .preview-img-box img {
            display: none;               /* hidden until there is a picture */
            width: 100%;
            height: 100%;
            object-fit: cover;
        }

        .preview-title {
            margin-bottom: 6px;
            font-size: 18px;
            font-weight: 700;
        }

        .preview-desc {
            margin-bottom: 16px;
            color: var(--grey);
            font-size: 13px;
            line-height: 1.5;
        }

        /* Big text box for the description */
        .input.textarea {
            height: auto;
            padding: 10px 12px;
            font-family: inherit;
            resize: vertical;
        }

        /* "Choose file" button */
        .file-input {
            width: 100%;
            margin-bottom: 14px;
            font-size: 12px;
        }

        /* ---------- Number boxes with a little icon ---------- */
        .stat-top {
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
        }

        .stat-top svg {
            flex-shrink: 0;
            color: var(--blue);
        }

        /* ---------- Sidebar: a little icon before every menu name ---------- */
        .sidebar a.menu-item {
            display: flex;
            align-items: center;
            gap: 12px;                  /* space between the icon and the name */
        }

        .menu-icon {
            width: 18px;
            height: 18px;
            flex-shrink: 0;
            fill: none;
            stroke: currentColor;       /* the icon has the same color as the name */
            stroke-width: 2;
            stroke-linecap: round;
            stroke-linejoin: round;
        }

        /* ---------- The avatar + drop-down menu (top right) ---------- */
        .user-menu {
            position: relative;
        }

        .user-btn {
            display: flex;
            align-items: center;
            padding: 0;
            background: none;
            border: none;
            cursor: pointer;
        }

        .user-btn .avatar {
            margin-left: 0;
        }

        .user-dropdown {
            display: none;                 /* hidden until JavaScript adds "open" */
            position: absolute;
            right: 0;
            top: 44px;
            min-width: 180px;
            padding: 6px;
            background: white;
            border: 1px solid var(--line);
            border-radius: 10px;
            box-shadow: 0 10px 30px rgba(15, 27, 76, 0.15);
            z-index: 50;
        }

        .user-dropdown.open {
            display: block;
        }

        .user-dropdown a {
            display: block;
            padding: 10px 12px;
            border-radius: 6px;
            color: var(--text);
            font-size: 13px;
            text-decoration: none;
        }

        .user-dropdown a:hover {
            background: #F1F5F9;
        }

        .user-dropdown a.logout {
            color: #DC2626;
        }

        /* ---------- Topic box: it is a typing box with a list, so we draw the arrow ourselves.
                      Now the arrow is ALWAYS shown (not only when the mouse is over the box) ---------- */
        input[list] {
            padding-right: 40px;
            background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='%2364748B' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpolyline points='6 9 12 15 18 9'/%3E%3C/svg%3E");
            background-repeat: no-repeat;
            background-position: right 16px center;
            background-size: 14px;
        }

        /* hide the browser's own little arrow (ours is shown instead, the click still works) */
        input[list]::-webkit-calendar-picker-indicator {
            opacity: 0;
            cursor: pointer;
        }

        /* ---------- Two small boxes side by side (Weeks + Hours per week) ---------- */
        .form-row {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 12px;
        }

        /* Small grey line under a box */
        .form-hint {
            margin: -6px 0 14px 0;
            color: var(--grey);
            font-size: 12px;
        }
    </style>

    <!-- ==================================================
         JAVASCRIPT : controls the popups
         ================================================== -->
    <script type="text/javascript">

        var currentId = 0;   // the course we clicked on

        // ----- Show / hide a popup -----
        function openModal(id)  { document.getElementById(id).classList.add('open'); }
        function closeModal(id) { document.getElementById(id).classList.remove('open'); }

        // ----- Helpers -----
        function rowOf(id)       { return document.getElementById('row' + id); }                   // the table row of a course
        function show(id, text)  { document.getElementById(id).innerText = text; }                 // put text in a box
        function orDash(text)    { return (text == null || text == '') ? '—' : text; }             // empty -> a dash

        // The Status column holds "published" or "draft".
        // Anything that is not "published" is shown as a draft.
        function cleanStatus(text) { return (text == 'published') ? 'published' : 'draft'; }

        // Put the green "Published" or red "Draft" badge in a box
        function showStatus(id, text) {
            var box = document.getElementById(id);
            var status = cleanStatus(text);
            box.innerText = (status == 'published') ? 'Published' : 'Draft';
            box.className = 'badge ' + status;
        }

        // The references are saved in the database like a list:  ["Book one", "Book two"]
        // This turns that list into plain lines (one reference per line)
        function referencesToLines(text) {
            if (text == null || text == '') { return ''; }
            try {
                var list = JSON.parse(text);
                if (Array.isArray(list)) { return list.join('\n'); }
            } catch (e) { }
            return text;     // not a list: just show the text as it is
        }

        // Put a picture in a picture box (or show the words "No picture" when there is none)
        function setPicture(imgId, textId, link) {
            var picture = document.getElementById(imgId);
            var words = document.getElementById(textId);

            if (link != '') {
                picture.src = link;
                picture.style.display = 'block';
                words.style.display = 'none';
            } else {
                picture.style.display = 'none';
                words.style.display = 'block';
            }
        }

        // When the admin chooses a picture file, show it right away in the Create popup
        function previewImage(input) {
            if (input.files && input.files[0]) {
                setPicture('addImg', 'addImgText', URL.createObjectURL(input.files[0]));
            } else {
                setPicture('addImg', 'addImgText', '');
            }
        }

        // When the admin chooses a NEW picture in the Edit popup, show it right away.
        // (If the choice is cleared, show the picture the course already has.)
        function previewEditImage(input) {
            if (input.files && input.files[0]) {
                setPicture('editImg', 'editImgText', URL.createObjectURL(input.files[0]));
            } else {
                setPicture('editImg', 'editImgText', rowOf(currentId).getAttribute('data-img'));
            }
        }

        // Reads the saved title, duration and qualification of a row and gives them back in small pieces.
        //   title    "SPM Chemistry: Chemical Bonds"  ->  level = SPM,  name = Chemical Bonds
        //   duration "10 Weeks (3hrs/week)"           ->  weeks = 10,   hours = 3
        function readRow(row) {
            var info = { level: '', name: row.getAttribute('data-title'), weeks: '', hours: '' };

            var parts = /^(SPM|IGCSE)\b[^:]*:\s*(.*)$/i.exec(info.name);
            if (parts) { info.level = parts[1].toUpperCase(); info.name = parts[2]; }
            else {
                // no "SPM ...:" in the title, so use the course_level column
                var saved = (row.getAttribute('data-level') || '').toUpperCase();
                if (saved == 'SPM' || saved == 'IGCSE') info.level = saved;
            }

            var d = /(\d+(?:\.\d+)?)\s*weeks?\s*\(\s*(\d+(?:\.\d+)?)\s*hrs?\s*\/\s*week\s*\)/i.exec(row.getAttribute('data-duration'));
            if (d) { info.weeks = String(parseInt(d[1])); info.hours = String(parseInt(d[2])); }

            return info;
        }

        // ----- POPUP 1 : course detail (when you click a row) -----
        function openDetail(id) {
            currentId = id;
            var row = rowOf(id);

            var info = readRow(row);

            show('dTitle',    row.getAttribute('data-title'));
            show('dName',     info.name);
            show('dLevel',    orDash(info.level));
            show('dTopic',    orDash(row.getAttribute('data-topic')));
            show('dWeeks',    info.weeks == '' ? '—' : info.weeks + (info.weeks == '1' ? ' week' : ' weeks'));
            show('dHours',    info.hours == '' ? '—' : info.hours + (info.hours == '1' ? ' hr/week' : ' hrs/week'));
            show('dDuration', orDash(row.getAttribute('data-duration')));
            show('dCreated',  row.getAttribute('data-created'));
            show('dId',       id);

            // Status (green / red) and the references (one per line)
            showStatus('dStatus', row.getAttribute('data-status'));
            show('dReferences', orDash(referencesToLines(row.getAttribute('data-reference'))));

            // The description (hidden when the course has none)
            var description = row.getAttribute('data-description');
            show('dDesc', description);
            document.getElementById('dDesc').style.display = (description == '') ? 'none' : 'block';

            // The badge: how many lessons
            var lessons = parseInt(row.getAttribute('data-lessons'));
            var badge = document.getElementById('dBadge');
            badge.innerText = lessons + (lessons == 1 ? ' lesson' : ' lessons');
            badge.className = 'badge ' + (lessons > 0 ? 'active' : 'pending');

            // The picture
            setPicture('dImg', 'dImgText', row.getAttribute('data-img'));

            openModal('detailModal');
        }

        // The "Edit Course" button inside the detail popup
        function editFromDetail() {
            closeModal('detailModal');
            openEdit(currentId);
        }

        // ----- The drop-down menu at the top right -----
        function toggleUserMenu(event) {
            event.stopPropagation();   // so the click does not reach "document" below
            document.getElementById('userDropdown').classList.toggle('open');
        }

        document.addEventListener('click', function () {
            document.getElementById('userDropdown').classList.remove('open');
        });

        // ----- The title is saved like:  SPM Biology: Cell Physiology & Organization -----
        //  = qualification (SPM / IGCSE)  +  topic  +  ":"  +  course name
        function buildTitle(level, topic, name) {
            if (level == '') { return name; }
            return level + (topic != '' ? ' ' + topic : '') + ': ' + name;
        }

        // Shows the admin how the title will look when saved (updates while typing)
        function showPreview(levelId, topicId, nameId, previewId) {
            var name = document.getElementById(nameId).value.trim();
            var text = '';

            if (name != '') {
                text = 'Saved as: ' + buildTitle(document.getElementById(levelId).value,
                                                 document.getElementById(topicId).value.trim(), name);
            }
            document.getElementById(previewId).innerText = text;
        }

        function previewAdd()  { showPreview('ddlAddLevel',  'ddlAddTopic',  'txtAddTitle',  'addPreview'); }
        function previewEdit() { showPreview('ddlEditLevel', 'ddlEditTopic', 'txtEditTitle', 'editPreview'); }

        window.addEventListener('load', function () { previewAdd(); previewEdit(); });

        // ----- POPUP 4 : delete course (asks "are you sure?") -----
        function openDelete(id) {
            document.getElementById('hfDeleteId').value = id;
            show('delName', rowOf(id).getAttribute('data-title'));
            openModal('deleteModal');
        }

        // ----- POPUP 2 : edit course -----
        function openEdit(id) {
            var row = rowOf(id);
            document.getElementById('hfEditId').value = id;

            currentId = id;
            var info = readRow(row);

            document.getElementById('ddlEditLevel').value = info.level;
            document.getElementById('txtEditTitle').value = info.name;
            document.getElementById('ddlEditTopic').value = row.getAttribute('data-topic');   // pick the topic from the list

            // Weeks + hours: the saved numbers, or "Select weeks" / "Select hours" when there is nothing saved
            document.getElementById('ddlEditWeeks').value = info.weeks;
            document.getElementById('ddlEditHours').value = info.hours;

            // Status ("draft" or "published") and the references (one per line)
            document.getElementById('ddlEditStatus').value = cleanStatus(row.getAttribute('data-status'));
            document.getElementById('txtEditReferences').value = referencesToLines(row.getAttribute('data-reference'));

            // The picture the course has now (the file box is cleared)
            setPicture('editImg', 'editImgText', row.getAttribute('data-img'));
            document.getElementById('fuEditImage').value = '';

            // The description box only exists when the table has a description column
            var box = document.getElementById('txtEditDescription');
            if (box != null) { box.value = row.getAttribute('data-description'); }

            previewEdit();
            openModal('editModal');
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
            <a class="menu-item active" href="coursesManagement.aspx">
                <svg class="menu-icon" viewBox="0 0 24 24"><path d="M2 3h6a4 4 0 014 4v14a3 3 0 00-3-3H2z" /><path d="M22 3h-6a4 4 0 00-4 4v14a3 3 0 013-3h7z" /></svg>
                Courses
            </a>
            <a class="menu-item" href="lessonManagement.aspx">
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
                    <h1>Course Management</h1>
                    <p>Review and maintain the learning catalogue</p>
                </div>
                <div class="user-menu">
                    <button type="button" class="user-btn" onclick="toggleUserMenu(event)" title="Account">
                        <span class="avatar"><%= AdminInitials %></span>
                    </button>

                    <!-- The 2 options -->
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
                            <div class="stat-label">All courses</div>
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M2 3h6a4 4 0 014 4v14a3 3 0 00-3-3H2z" /><path d="M22 3h-6a4 4 0 00-4 4v14a3 3 0 013-3h7z" /></svg>
                        </div>
                        <div class="stat-number"><asp:Label ID="lblTotal" runat="server"></asp:Label></div>
                        <div class="stat-note"><asp:Label ID="lblTotalNote" runat="server"></asp:Label></div>
                    </div>

                    <div class="card">
                        <div class="stat-top">
                            <div class="stat-label">Topics</div>
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="7" height="7" /><rect x="14" y="3" width="7" height="7" /><rect x="14" y="14" width="7" height="7" /><rect x="3" y="14" width="7" height="7" /></svg>
                        </div>
                        <div class="stat-number"><asp:Label ID="lblTopics" runat="server"></asp:Label></div>
                        <div class="stat-note">Different categories</div>
                    </div>

                    <div class="card">
                        <div class="stat-top">
                            <div class="stat-label">Biology</div>
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M11 20A7 7 0 019.8 6.1C15.5 5 17 4.48 19 2c1 2 2 4.18 2 8 0 5.5-4.78 10-10 10z" /><path d="M2 21c0-3 1.85-5.36 5.08-6C9.5 14.52 12 13 13 12" /></svg>
                        </div>
                        <div class="stat-number"><asp:Label ID="lblBiology" runat="server"></asp:Label></div>
                        <div class="stat-note"><asp:Label ID="lblBiologyNote" runat="server"></asp:Label></div>
                    </div>

                    <div class="card">
                        <div class="stat-top">
                            <div class="stat-label">Chemistry</div>
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M10 2v7.527a2 2 0 01-.211.896L4.72 20.55a1 1 0 00.9 1.45h12.76a1 1 0 00.9-1.45l-5.069-10.127A2 2 0 0114 9.527V2" /><path d="M8.5 2h7" /><path d="M7 16h10" /></svg>
                        </div>
                        <div class="stat-number"><asp:Label ID="lblChemistry" runat="server"></asp:Label></div>
                        <div class="stat-note"><asp:Label ID="lblChemistryNote" runat="server"></asp:Label></div>
                    </div>

                    <div class="card">
                        <div class="stat-top">
                            <div class="stat-label">Physics</div>
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="1" /><path d="M20.2 20.2c2.04-2.03.02-7.36-4.5-11.9-4.54-4.52-9.87-6.54-11.9-4.5-2.04 2.03-.02 7.36 4.5 11.9 4.54 4.52 9.87 6.54 11.9 4.5z" /><path d="M15.7 15.7c4.52-4.54 6.54-9.87 4.5-11.9-2.03-2.04-7.36-.02-11.9 4.5-4.52 4.54-6.54 9.87-4.5 11.9 2.03 2.04 7.36.02 11.9-4.5z" /></svg>
                        </div>
                        <div class="stat-number"><asp:Label ID="lblPhysics" runat="server"></asp:Label></div>
                        <div class="stat-note"><asp:Label ID="lblPhysicsNote" runat="server"></asp:Label></div>
                    </div>

                </div>


                <!-- ==============================================
                     COURSE TABLE CARD
                     ============================================== -->
                <div class="card flush">

                    <!-- Search + filters + Create Course button.
                         DefaultButton = pressing ENTER in the box clicks "Search" -->
                    <asp:Panel ID="pnlToolbar" runat="server" CssClass="toolbar" DefaultButton="btnSearch">

                        <asp:TextBox ID="txtSearch" runat="server" CssClass="input search-box" placeholder="Search course or topic"></asp:TextBox>

                        <!-- C# fills this dropdown with the topics from the database -->
                        <asp:DropDownList ID="ddlTopic" runat="server" CssClass="input" AutoPostBack="true" OnSelectedIndexChanged="Filter_Changed">
                        </asp:DropDownList>

                        <asp:DropDownList ID="ddlLessons" runat="server" CssClass="input" AutoPostBack="true" OnSelectedIndexChanged="Filter_Changed">
                            <asp:ListItem Text="All courses" Value=""></asp:ListItem>
                            <asp:ListItem Text="With lessons" Value="with"></asp:ListItem>
                            <asp:ListItem Text="Without lessons" Value="without"></asp:ListItem>
                        </asp:DropDownList>

                        <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn" OnClick="Filter_Changed" />

                        <div class="spacer"></div>

                        <!-- type="button" = just opens the popup, no server needed -->
                        <button type="button" class="btn btn-primary" onclick="openModal('addModal')">+ Create Course</button>

                    </asp:Panel>


                    <!-- The table (only shown when we found courses) -->
                    <asp:Panel ID="pnlTable" runat="server">

                        <table class="data">
                            <tr>
                                <th>ID</th>
                                <th>Course title</th>
                                <th>Topic</th>
                                <th>Duration</th>
                                <th>Lessons</th>
                                <th>Status</th>
                                <th>Created</th>
                                <th>Actions</th>
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


                    <!-- "No results found" (only shown when the search finds nothing) -->
                    <asp:Panel ID="pnlEmpty" runat="server" Visible="false">
                        <div class="empty-state">
                            <div class="empty-icon">
                                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round">
                                    <circle cx="11" cy="11" r="7" /><line x1="21" y1="21" x2="16.5" y2="16.5" />
                                </svg>
                            </div>
                            <div class="empty-title">No results found</div>
                            Try another keyword, clear the search, or change the filters to see all courses.
                        </div>
                    </asp:Panel>

                </div>

            </div>
        </div>


        <!-- A hidden list of topics. The Topic boxes use it to suggest topics while you type. -->
        <datalist id="topicList">
            <asp:Literal ID="litTopicOptions" runat="server"></asp:Literal>
        </datalist>


        <!-- ==================================================
             POPUP 1 : COURSE DETAIL  (opens when you click a row)
             Same look as the Figma "course preview" card.
             ================================================== -->
        <div class="modal-back" id="detailModal">
            <div class="modal-box">

                <div class="modal-head">
                    <h3>Course Preview</h3>
                    <span id="dBadge" class="badge active">0 lessons</span>
                </div>

                <div class="preview-card">

                    <!-- the picture -->
                    <div class="preview-img-box">
                        <img id="dImg" alt="Course picture" />
                        <span id="dImgText">No picture yet</span>
                    </div>

                    <div class="preview-title" id="dTitle"></div>
                    <div class="preview-desc" id="dDesc"></div>

                    <div class="modal-buttons">
                        <button type="button" class="btn btn-primary" onclick="editFromDetail()">Edit Course</button>
                        <button type="button" class="btn" onclick="closeModal('detailModal')">Close</button>
                    </div>
                </div>

                <!-- Everything that is in the Edit Course popup, shown as plain text -->
                <div class="info-box">
                    <div class="info-label">Course name</div>
                    <div class="info-value" id="dName"></div>

                    <div class="info-label">Qualification</div>
                    <div class="info-value" id="dLevel"></div>

                    <div class="info-label">Topic</div>
                    <div class="info-value" id="dTopic"></div>

                    <div class="info-label">Duration (weeks)</div>
                    <div class="info-value" id="dWeeks"></div>

                    <div class="info-label">Hours per week</div>
                    <div class="info-value" id="dHours"></div>

                    <div class="info-label">Duration (saved as)</div>
                    <div class="info-value" id="dDuration"></div>

                    <div class="info-label">Status</div>
                    <div class="info-value"><span id="dStatus" class="badge draft">Draft</span></div>

                    <div class="info-label">References</div>
                    <div class="info-value" id="dReferences"></div>

                    <div class="info-label">Created</div>
                    <div class="info-value" id="dCreated"></div>

                    <div class="info-label">Course ID</div>
                    <div class="info-value" id="dId"></div>
                </div>

            </div>
        </div>


        <!-- ==================================================
             POPUP 2 : EDIT COURSE
             ClientIDMode="Static" = the ID stays the same,
             so JavaScript can find it easily.
             ================================================== -->
        <div class="modal-back" id="editModal">
            <div class="modal-box">

                <div class="modal-head">
                    <h3>Edit Course</h3>
                </div>

                <asp:HiddenField ID="hfEditId" runat="server" ClientIDMode="Static" />

                <!-- The course picture (same look as the Course Preview).
                     Choose a file here to add or change the picture. -->
                <div class="preview-img-box">
                    <img id="editImg" alt="Course picture" />
                    <span id="editImgText">No picture yet</span>
                </div>
                <asp:FileUpload ID="fuEditImage" runat="server" ClientIDMode="Static" CssClass="file-input" accept="image/*" onchange="previewEditImage(this)" />

                <div class="form-field">
                    <label>Course name</label>
                    <asp:TextBox ID="txtEditTitle" runat="server" ClientIDMode="Static" CssClass="input" oninput="previewEdit()"></asp:TextBox>
                </div>

                <!-- Only shown when the table has a description column -->
                <asp:Panel ID="pnlEditDesc" runat="server" CssClass="form-field">
                    <label>Description</label>
                    <asp:TextBox ID="txtEditDescription" runat="server" ClientIDMode="Static" TextMode="MultiLine" Rows="3" CssClass="input textarea"></asp:TextBox>
                </asp:Panel>

                <div class="form-field">
                    <label>Qualification</label>
                    <asp:DropDownList ID="ddlEditLevel" runat="server" ClientIDMode="Static" CssClass="input" onchange="previewEdit()">
                        <asp:ListItem Text="Not set" Value=""></asp:ListItem>
                        <asp:ListItem Text="SPM" Value="SPM"></asp:ListItem>
                        <asp:ListItem Text="IGCSE" Value="IGCSE"></asp:ListItem>
                    </asp:DropDownList>
                </div>

                <div class="form-field">
                    <label>Topic</label>
                    <!-- A dropdown (the admin picks a topic, cannot type). C# fills it with the same topics as the Create popup list. -->
                    <asp:DropDownList ID="ddlEditTopic" runat="server" ClientIDMode="Static" CssClass="input" onchange="previewEdit()"></asp:DropDownList>
                </div>

                <div class="form-hint" id="editPreview"></div>

                <!-- The duration is saved like:  4 weeks (3 hrs/week) -->
                <div class="form-row">
                    <div class="form-field">
                        <label>Duration (weeks, 1-24)</label>
                        <asp:DropDownList ID="ddlEditWeeks" runat="server" ClientIDMode="Static" CssClass="input"><asp:ListItem Value="">Select weeks</asp:ListItem><asp:ListItem Value="1">1</asp:ListItem><asp:ListItem Value="2">2</asp:ListItem><asp:ListItem Value="3">3</asp:ListItem><asp:ListItem Value="4">4</asp:ListItem><asp:ListItem Value="5">5</asp:ListItem><asp:ListItem Value="6">6</asp:ListItem><asp:ListItem Value="7">7</asp:ListItem><asp:ListItem Value="8">8</asp:ListItem><asp:ListItem Value="9">9</asp:ListItem><asp:ListItem Value="10">10</asp:ListItem><asp:ListItem Value="11">11</asp:ListItem><asp:ListItem Value="12">12</asp:ListItem><asp:ListItem Value="13">13</asp:ListItem><asp:ListItem Value="14">14</asp:ListItem><asp:ListItem Value="15">15</asp:ListItem><asp:ListItem Value="16">16</asp:ListItem><asp:ListItem Value="17">17</asp:ListItem><asp:ListItem Value="18">18</asp:ListItem><asp:ListItem Value="19">19</asp:ListItem><asp:ListItem Value="20">20</asp:ListItem><asp:ListItem Value="21">21</asp:ListItem><asp:ListItem Value="22">22</asp:ListItem><asp:ListItem Value="23">23</asp:ListItem><asp:ListItem Value="24">24</asp:ListItem></asp:DropDownList>
                    </div>
                    <div class="form-field">
                        <label>Hours per week (1-24)</label>
                        <asp:DropDownList ID="ddlEditHours" runat="server" ClientIDMode="Static" CssClass="input"><asp:ListItem Value="">Select hours</asp:ListItem><asp:ListItem Value="1">1</asp:ListItem><asp:ListItem Value="2">2</asp:ListItem><asp:ListItem Value="3">3</asp:ListItem><asp:ListItem Value="4">4</asp:ListItem><asp:ListItem Value="5">5</asp:ListItem><asp:ListItem Value="6">6</asp:ListItem><asp:ListItem Value="7">7</asp:ListItem><asp:ListItem Value="8">8</asp:ListItem><asp:ListItem Value="9">9</asp:ListItem><asp:ListItem Value="10">10</asp:ListItem><asp:ListItem Value="11">11</asp:ListItem><asp:ListItem Value="12">12</asp:ListItem><asp:ListItem Value="13">13</asp:ListItem><asp:ListItem Value="14">14</asp:ListItem><asp:ListItem Value="15">15</asp:ListItem><asp:ListItem Value="16">16</asp:ListItem><asp:ListItem Value="17">17</asp:ListItem><asp:ListItem Value="18">18</asp:ListItem><asp:ListItem Value="19">19</asp:ListItem><asp:ListItem Value="20">20</asp:ListItem><asp:ListItem Value="21">21</asp:ListItem><asp:ListItem Value="22">22</asp:ListItem><asp:ListItem Value="23">23</asp:ListItem><asp:ListItem Value="24">24</asp:ListItem></asp:DropDownList>
                    </div>
                </div>

                <!-- Draft = students cannot see it.  Published = students can see it. -->
                <div class="form-field">
                    <label>Status</label>
                    <asp:DropDownList ID="ddlEditStatus" runat="server" ClientIDMode="Static" CssClass="input">
                        <asp:ListItem Text="Unpublished (draft)" Value="draft"></asp:ListItem>
                        <asp:ListItem Text="Published" Value="published"></asp:ListItem>
                    </asp:DropDownList>
                </div>

                <!-- One reference (book) per line. C# saves them as a list. -->
                <div class="form-field">
                    <label>References (one per line)</label>
                    <asp:TextBox ID="txtEditReferences" runat="server" ClientIDMode="Static" TextMode="MultiLine" Rows="4" CssClass="input textarea" placeholder="Author (year). Book title. Publisher."></asp:TextBox>
                </div>

                <div class="modal-buttons">
                    <button type="button" class="btn" onclick="closeModal('editModal')">Cancel</button>
                    <asp:Button ID="btnSaveEdit" runat="server" Text="Save changes" CssClass="btn btn-primary" OnClick="btnSaveEdit_Click" />
                </div>

            </div>
        </div>


        <!-- ==================================================
             POPUP 3 : CREATE COURSE
             Same look as the Figma "course preview" card:
             picture on top, then title, description and buttons.
             ================================================== -->
        <div class="modal-back" id="addModal">
            <div class="modal-box">

                <div class="modal-head">
                    <h3>Create Course</h3>
                    <span class="badge new">New</span>
                </div>

                <div class="preview-card">

                    <!-- the picture (shows the file you choose) -->
                    <div class="preview-img-box">
                        <img id="addImg" alt="Course picture" />
                        <span id="addImgText">Course picture preview</span>
                    </div>

                    <!-- choose a picture file from the computer -->
                    <asp:FileUpload ID="fuAddImage" runat="server" CssClass="file-input" accept="image/*" onchange="previewImage(this)" />

                    <div class="form-field">
                        <label>Course name</label>
                        <asp:TextBox ID="txtAddTitle" runat="server" ClientIDMode="Static" CssClass="input" placeholder="e.g. Cell Physiology &amp; Organization" oninput="previewAdd()"></asp:TextBox>
                    </div>

                    <!-- Only shown when the table has a description column -->
                    <asp:Panel ID="pnlAddDesc" runat="server" CssClass="form-field">
                        <label>Description</label>
                        <asp:TextBox ID="txtAddDescription" runat="server" TextMode="MultiLine" Rows="3" CssClass="input textarea" placeholder="A short introduction to the course"></asp:TextBox>
                    </asp:Panel>

                    <div class="form-field">
                        <label>Qualification</label>
                        <asp:DropDownList ID="ddlAddLevel" runat="server" ClientIDMode="Static" CssClass="input" onchange="previewAdd()">
                            <asp:ListItem Text="SPM" Value="SPM"></asp:ListItem>
                            <asp:ListItem Text="IGCSE" Value="IGCSE"></asp:ListItem>
                        </asp:DropDownList>
                    </div>

                    <div class="form-field">
                        <label>Topic</label>
                        <!-- A dropdown (the admin picks a topic, cannot type). C# fills it with the topics from the database. -->
                        <asp:DropDownList ID="ddlAddTopic" runat="server" ClientIDMode="Static" CssClass="input" onchange="previewAdd()"></asp:DropDownList>
                    </div>

                    <div class="form-hint" id="addPreview"></div>

                    <!-- The duration is saved like:  4 weeks (3 hrs/week) -->
                    <div class="form-row">
                        <div class="form-field">
                            <label>Duration (weeks, 1-24)</label>
                            <asp:DropDownList ID="ddlAddWeeks" runat="server" ClientIDMode="Static" CssClass="input"><asp:ListItem Value="">Select weeks</asp:ListItem><asp:ListItem Value="1">1</asp:ListItem><asp:ListItem Value="2">2</asp:ListItem><asp:ListItem Value="3">3</asp:ListItem><asp:ListItem Value="4">4</asp:ListItem><asp:ListItem Value="5">5</asp:ListItem><asp:ListItem Value="6">6</asp:ListItem><asp:ListItem Value="7">7</asp:ListItem><asp:ListItem Value="8">8</asp:ListItem><asp:ListItem Value="9">9</asp:ListItem><asp:ListItem Value="10">10</asp:ListItem><asp:ListItem Value="11">11</asp:ListItem><asp:ListItem Value="12">12</asp:ListItem><asp:ListItem Value="13">13</asp:ListItem><asp:ListItem Value="14">14</asp:ListItem><asp:ListItem Value="15">15</asp:ListItem><asp:ListItem Value="16">16</asp:ListItem><asp:ListItem Value="17">17</asp:ListItem><asp:ListItem Value="18">18</asp:ListItem><asp:ListItem Value="19">19</asp:ListItem><asp:ListItem Value="20">20</asp:ListItem><asp:ListItem Value="21">21</asp:ListItem><asp:ListItem Value="22">22</asp:ListItem><asp:ListItem Value="23">23</asp:ListItem><asp:ListItem Value="24">24</asp:ListItem></asp:DropDownList>
                        </div>
                        <div class="form-field">
                            <label>Hours per week (1-24)</label>
                            <asp:DropDownList ID="ddlAddHours" runat="server" ClientIDMode="Static" CssClass="input"><asp:ListItem Value="">Select hours</asp:ListItem><asp:ListItem Value="1">1</asp:ListItem><asp:ListItem Value="2">2</asp:ListItem><asp:ListItem Value="3">3</asp:ListItem><asp:ListItem Value="4">4</asp:ListItem><asp:ListItem Value="5">5</asp:ListItem><asp:ListItem Value="6">6</asp:ListItem><asp:ListItem Value="7">7</asp:ListItem><asp:ListItem Value="8">8</asp:ListItem><asp:ListItem Value="9">9</asp:ListItem><asp:ListItem Value="10">10</asp:ListItem><asp:ListItem Value="11">11</asp:ListItem><asp:ListItem Value="12">12</asp:ListItem><asp:ListItem Value="13">13</asp:ListItem><asp:ListItem Value="14">14</asp:ListItem><asp:ListItem Value="15">15</asp:ListItem><asp:ListItem Value="16">16</asp:ListItem><asp:ListItem Value="17">17</asp:ListItem><asp:ListItem Value="18">18</asp:ListItem><asp:ListItem Value="19">19</asp:ListItem><asp:ListItem Value="20">20</asp:ListItem><asp:ListItem Value="21">21</asp:ListItem><asp:ListItem Value="22">22</asp:ListItem><asp:ListItem Value="23">23</asp:ListItem><asp:ListItem Value="24">24</asp:ListItem></asp:DropDownList>
                        </div>
                    </div>

                    <!-- Draft = students cannot see it.  Published = students can see it. -->
                    <div class="form-field">
                        <label>Status</label>
                        <asp:DropDownList ID="ddlAddStatus" runat="server" ClientIDMode="Static" CssClass="input">
                            <asp:ListItem Text="Unpublished (draft)" Value="draft"></asp:ListItem>
                            <asp:ListItem Text="Published" Value="published"></asp:ListItem>
                        </asp:DropDownList>
                    </div>

                    <!-- One reference (book) per line. C# saves them as a list. -->
                    <div class="form-field">
                        <label>References (one per line)</label>
                        <asp:TextBox ID="txtAddReferences" runat="server" TextMode="MultiLine" Rows="4" CssClass="input textarea" placeholder="Author (year). Book title. Publisher."></asp:TextBox>
                    </div>

                    <div class="form-error">
                        <asp:Label ID="lblAddError" runat="server"></asp:Label>
                    </div>

                    <div class="modal-buttons">
                        <asp:Button ID="btnAddCourse" runat="server" Text="Create Course" CssClass="btn btn-primary" OnClick="btnAddCourse_Click" />
                        <button type="button" class="btn" onclick="closeModal('addModal')">Cancel</button>
                    </div>
                </div>

            </div>
        </div>


        <!-- ==================================================
             POPUP 4 : DELETE COURSE (asks "are you sure?")
             ================================================== -->
        <div class="modal-back" id="deleteModal">
            <div class="modal-box danger">

                <div class="modal-head">
                    <h3>Delete Course</h3>
                </div>

                <asp:HiddenField ID="hfDeleteId" runat="server" ClientIDMode="Static" />

                <div class="modal-title">Are you sure you want to delete <span id="delName"></span>?</div>
                <p class="modal-text">This course will be permanently removed from the database, together with its lessons, quizzes, enrolments, certificates and everything else linked to it. This cannot be undone.</p>

                <div class="modal-buttons">
                    <button type="button" class="btn" onclick="closeModal('deleteModal')">Cancel</button>
                    <asp:Button ID="btnConfirmDelete" runat="server" Text="Confirm" CssClass="btn btn-danger" OnClick="btnConfirmDelete_Click" />
                </div>

            </div>
        </div>

    </form>
</body>
</html>
