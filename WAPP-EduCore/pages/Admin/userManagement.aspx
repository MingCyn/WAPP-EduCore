<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="userManagement.aspx.cs" Inherits="WAPP_EduCore.pages.Admin.WebForm2" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">

<head runat="server">
    <title>EduCore - User Management</title>

    <!-- Font -->
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet" />

    <!-- Our own styles (colors, cards, tables, popups...) -->
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />

    <!-- Small styles that live in this page (so they work even if admin.css was not updated) -->
    <style type="text/css">

        /* ALL dropdowns: move the little arrow away from the right edge.
           Want it even more to the left? Make the 20px bigger, for example 28px. */
        select.input {
            appearance: none;                 /* hide the browser's own arrow */
            -webkit-appearance: none;
            padding-right: 44px;              /* keep the words away from the arrow */
            background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='%2364748B' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpolyline points='6 9 12 15 18 9'/%3E%3C/svg%3E");
            background-repeat: no-repeat;
            background-position: right 20px center;   /* 20px from the right edge */
            background-size: 14px;
        }

        /* Password box with the eye INSIDE it (same idea as the calendar icon) */
        .password-wrap {
            position: relative;           /* the eye is placed inside this box */
        }

        .password-wrap .input {
            display: block;
            padding-right: 40px;          /* keep typed letters away from the eye */
        }

        .eye-btn {
            position: absolute;
            top: 0;
            bottom: 0;                    /* same height as the box, so the eye is centered */
            right: 8px;                   /* a little space from the right edge */
            display: flex;
            align-items: center;
            justify-content: center;
            width: 28px;
            padding: 0;
            background: none;
            border: none;
            color: var(--grey);
            cursor: pointer;
        }

        .eye-btn:hover {
            color: var(--blue);
        }

        /* ---------- DROPDOWN ARROW (the little v) ----------
           Moves the arrow a bit to the LEFT, away from the right edge.
           Works for every dropdown with class="input" on this page. */
        select.input {
            appearance: none;                 /* hide the browser's own arrow */
            -webkit-appearance: none;
            padding-right: 40px;              /* keep the text away from the arrow */
            background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='%2364748B' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpolyline points='6 9 12 15 18 9'/%3E%3C/svg%3E");
            background-repeat: no-repeat;
            background-position: right 16px center;   /* 16px from the right edge. Make it bigger (for example 22px) to move the arrow more to the left */
            background-size: 14px;
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

        /* ---------- Date box: a hidden real calendar, opened by the little calendar button ---------- */
        .hidden-date {
            position: absolute;
            right: 8px;
            bottom: 0;
            width: 1px;
            height: 1px;
            opacity: 0;
            pointer-events: none;
        }
    </style>

    <!-- ==================================================
         JAVASCRIPT : controls the popups
         (no server needed, the browser does this by itself)
         ================================================== -->
    <script type="text/javascript">

        var currentId = 0;   // the user we clicked on

        // ----- Show / hide a popup -----
        function openModal(id)  { document.getElementById(id).classList.add('open'); }
        function closeModal(id) { document.getElementById(id).classList.remove('open'); }

        // ----- Helpers -----
        function rowOf(id)       { return document.getElementById('row' + id); }          // the table row of a user
        function show(id, text)  { document.getElementById(id).innerText = text; }        // put text in a box

        // ----- POPUP 1 : user detail (when you click a row) -----
        function openDetail(id) {
            currentId = id;
            var row = rowOf(id);

            show('dName',   row.getAttribute('data-name'));
            show('dRole',   row.getAttribute('data-roletext'));
            show('dEmail',  row.getAttribute('data-email'));
            show('dDob',    row.getAttribute('data-dob'));
            show('dGender', row.getAttribute('data-gender'));
            show('dJoined', row.getAttribute('data-joined'));
            show('dId',     id);          // just the number, for example 5

            openModal('detailModal');
        }

        // The "Edit User" button inside the detail popup
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

        // ----- DATE OF BIRTH  (always written as day/month/year, like 30/05/2012) -----

        // 2012-05-30  ->  30/05/2012
        function toDisplayDate(iso) {
            if (!iso) { return ''; }
            var p = iso.split('-');
            return p[2] + '/' + p[1] + '/' + p[0];
        }

        // 30/05/2012  ->  2012-05-30   (empty when the text is not a full date yet)
        function toIsoDate(text) {
            var m = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(text);
            return m ? (m[3] + '-' + m[2] + '-' + m[1]) : '';
        }

        // While typing: only numbers are kept, and the "/" is added by itself
        function maskDate(box) {
            var v = box.value.replace(/[^\d]/g, '').substring(0, 8);
            if (v.length > 4)      { v = v.substring(0, 2) + '/' + v.substring(2, 4) + '/' + v.substring(4); }
            else if (v.length > 2) { v = v.substring(0, 2) + '/' + v.substring(2); }
            box.value = v;
        }

        // The little calendar button: open the real calendar
        function openCalendar(boxId, pickerId) {
            var picker = document.getElementById(pickerId);
            picker.value = toIsoDate(document.getElementById(boxId).value);   // start on the date already typed
            if (picker.showPicker) { picker.showPicker(); } else { picker.click(); }
        }

        // A date was picked in the calendar: write it in the box as day/month/year
        function pickedDate(pickerId, boxId) {
            document.getElementById(boxId).value = toDisplayDate(document.getElementById(pickerId).value);
        }

        // ----- POPUP 4 : delete user (asks "are you sure?") -----
        function openDelete(id) {
            var row = rowOf(id);
            document.getElementById('hfDeleteId').value = id;
            show('delName', row.getAttribute('data-name'));
            openModal('deleteModal');
        }

        // ----- POPUP 2 : edit user -----
        function openEdit(id) {
            var row = rowOf(id);
            document.getElementById('hfEditId').value     = id;
            document.getElementById('txtEditName').value  = row.getAttribute('data-name');
            document.getElementById('txtEditEmail').value = row.getAttribute('data-email');
            document.getElementById('txtEditDob').value   = toDisplayDate(row.getAttribute('data-dobraw'));   // 2012-05-30 -> 30/05/2012
            document.getElementById('ddlEditGender').value = row.getAttribute('data-genderraw');
            document.getElementById('ddlEditRole').value  = row.getAttribute('data-role');
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
            <a class="menu-item active" href="userManagement.aspx">
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
                    <h1>User Management</h1>
                    <p>Manage access and roles</p>
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
                            <div class="stat-label">Total users</div>
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="M23 21v-2a4 4 0 00-3-3.87M16 3.13a4 4 0 010 7.75" /></svg>
                        </div>
                        <div class="stat-number"><asp:Label ID="lblTotal" runat="server"></asp:Label></div>
                        <div class="stat-note">All registered accounts</div>
                    </div>

                    <div class="card">
                        <div class="stat-top">
                            <div class="stat-label">New this month</div>
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M16 21v-2a4 4 0 00-4-4H6a4 4 0 00-4 4v2" /><circle cx="9" cy="7" r="4" /><line x1="20" y1="8" x2="20" y2="14" /><line x1="17" y1="11" x2="23" y2="11" /></svg>
                        </div>
                        <div class="stat-number"><asp:Label ID="lblNew" runat="server"></asp:Label></div>
                        <div class="stat-note">Joined this month</div>
                    </div>

                    <div class="card">
                        <div class="stat-top">
                            <div class="stat-label">Students</div>
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 10L12 5 2 10l10 5 10-5z" /><path d="M6 12v5c3 3 9 3 12 0v-5" /></svg>
                        </div>
                        <div class="stat-number"><asp:Label ID="lblStudents" runat="server"></asp:Label></div>
                        <div class="stat-note"><asp:Label ID="lblStudentsNote" runat="server"></asp:Label></div>
                    </div>

                    <div class="card">
                        <div class="stat-top">
                            <div class="stat-label">Tutors</div>
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M16 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" /><circle cx="8.5" cy="7" r="4" /><polyline points="17 11 19 13 23 9" /></svg>
                        </div>
                        <div class="stat-number"><asp:Label ID="lblTutors" runat="server"></asp:Label></div>
                        <div class="stat-note"><asp:Label ID="lblTutorsNote" runat="server"></asp:Label></div>
                    </div>

                    <div class="card">
                        <div class="stat-top">
                            <div class="stat-label">Administrators</div>
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" /></svg>
                        </div>
                        <div class="stat-number"><asp:Label ID="lblAdmins" runat="server"></asp:Label></div>
                        <div class="stat-note"><asp:Label ID="lblAdminsNote" runat="server"></asp:Label></div>
                    </div>

                </div>


                <!-- ==============================================
                     USER TABLE CARD
                     ============================================== -->
                <div class="card flush">

                    <!-- Search + filter + Add User button.
                         DefaultButton = pressing ENTER in the box clicks "Search" -->
                    <asp:Panel ID="pnlToolbar" runat="server" CssClass="toolbar" DefaultButton="btnSearch">

                        <asp:TextBox ID="txtSearch" runat="server" CssClass="input search-box" placeholder="Search name or email"></asp:TextBox>

                        <asp:DropDownList ID="ddlRole" runat="server" CssClass="input" AutoPostBack="true" OnSelectedIndexChanged="Filter_Changed">
                            <asp:ListItem Text="All roles" Value=""></asp:ListItem>
                            <asp:ListItem Text="Student" Value="Student"></asp:ListItem>
                            <asp:ListItem Text="Tutor" Value="Tutor"></asp:ListItem>
                            <asp:ListItem Text="Administrator" Value="Admin"></asp:ListItem>
                        </asp:DropDownList>

                        <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn" OnClick="Filter_Changed" />

                        <div class="spacer"></div>

                        <!-- type="button" = just opens the popup, no server needed -->
                        <button type="button" class="btn btn-primary" onclick="openModal('addModal')">+ Add User</button>

                    </asp:Panel>


                    <!-- The table (only shown when we found users) -->
                    <asp:Panel ID="pnlTable" runat="server">

                        <table class="data">
                            <tr>
                                <th>ID</th>
                                <th>User name</th>
                                <th>Email</th>
                                <th>Date of birth</th>
                                <th>Gender</th>
                                <th>Role</th>
                                <th>Registration date</th>
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


                    <!-- "No results found" (only shown when the search finds nobody) -->
                    <asp:Panel ID="pnlEmpty" runat="server" Visible="false">
                        <div class="empty-state">
                            <div class="empty-icon">
                                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round">
                                    <circle cx="11" cy="11" r="7" /><line x1="21" y1="21" x2="16.5" y2="16.5" />
                                </svg>
                            </div>
                            <div class="empty-title">No results found</div>
                            Try a different name or email to find the user you're looking for.
                        </div>
                    </asp:Panel>

                </div>

            </div>
        </div>


        <!-- ==================================================
             POPUP 1 : USER DETAIL  (opens when you click a row)
             ================================================== -->
        <div class="modal-back" id="detailModal">
            <div class="modal-box">

                <div class="modal-head">
                    <h3>User detail</h3>
                </div>

                <div class="modal-title" id="dName"></div>
                <p class="modal-text">Account information saved for this user.</p>

                <div class="info-box">
                    <div class="info-label">Role</div>
                    <div class="info-value" id="dRole"></div>

                    <div class="info-label">Email</div>
                    <div class="info-value" id="dEmail"></div>

                    <div class="info-label">Date of birth</div>
                    <div class="info-value" id="dDob"></div>

                    <div class="info-label">Gender</div>
                    <div class="info-value" id="dGender"></div>

                    <div class="info-label">Registered</div>
                    <div class="info-value" id="dJoined"></div>

                    <div class="info-label">User ID</div>
                    <div class="info-value" id="dId"></div>
                </div>

                <div class="modal-buttons">
                    <button type="button" class="btn" onclick="editFromDetail()">Edit User</button>
                    <button type="button" class="btn" onclick="closeModal('detailModal')">Close</button>
                </div>

            </div>
        </div>


        <!-- ==================================================
             POPUP 2 : EDIT USER
             ClientIDMode="Static" = the ID stays the same,
             so JavaScript can find it easily.
             ================================================== -->
        <div class="modal-back" id="editModal">
            <div class="modal-box">

                <div class="modal-head">
                    <h3>Edit User</h3>
                </div>

                <asp:HiddenField ID="hfEditId" runat="server" ClientIDMode="Static" />

                <div class="form-field">
                    <label>Full name</label>
                    <asp:TextBox ID="txtEditName" runat="server" ClientIDMode="Static" CssClass="input"></asp:TextBox>
                </div>

                <div class="form-field">
                    <label>Email address</label>
                    <asp:TextBox ID="txtEditEmail" runat="server" ClientIDMode="Static" CssClass="input"></asp:TextBox>
                </div>

                <div class="form-field">
                    <label>Date of birth (day/month/year)</label>
                    <!-- Date box + little calendar button inside it (same idea as the password eye) -->
                    <div class="password-wrap">
                        <asp:TextBox ID="txtEditDob" runat="server" ClientIDMode="Static" CssClass="input" MaxLength="10" placeholder="dd/mm/yyyy" oninput="maskDate(this)"></asp:TextBox>

                        <button type="button" class="eye-btn" onclick="openCalendar('txtEditDob', 'editDobPicker')" title="Pick a date">
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="4" width="18" height="18" rx="2" /><line x1="16" y1="2" x2="16" y2="6" /><line x1="8" y1="2" x2="8" y2="6" /><line x1="3" y1="10" x2="21" y2="10" /></svg>
                        </button>

                        <!-- the real calendar (hidden) -->
                        <input type="date" id="editDobPicker" class="hidden-date" tabindex="-1" onchange="pickedDate('editDobPicker', 'txtEditDob')" />
                    </div>
                </div>

                <div class="form-field">
                    <label>Gender</label>
                    <asp:DropDownList ID="ddlEditGender" runat="server" ClientIDMode="Static" CssClass="input">
                        <asp:ListItem Text="Not set" Value=""></asp:ListItem>
                        <asp:ListItem Text="Male" Value="Male"></asp:ListItem>
                        <asp:ListItem Text="Female" Value="Female"></asp:ListItem>
                    </asp:DropDownList>
                </div>

                <div class="form-field">
                    <label>Role</label>
                    <asp:DropDownList ID="ddlEditRole" runat="server" ClientIDMode="Static" CssClass="input">
                        <asp:ListItem Text="Student" Value="Student"></asp:ListItem>
                        <asp:ListItem Text="Tutor" Value="Tutor"></asp:ListItem>
                        <asp:ListItem Text="Administrator" Value="Admin"></asp:ListItem>
                    </asp:DropDownList>
                </div>

                <div class="modal-buttons">
                    <button type="button" class="btn" onclick="closeModal('editModal')">Cancel</button>
                    <asp:Button ID="btnSaveEdit" runat="server" Text="Save changes" CssClass="btn btn-primary" OnClick="btnSaveEdit_Click" />
                </div>

            </div>
        </div>


        <!-- ==================================================
             POPUP 3 : ADD USER
             ================================================== -->
        <div class="modal-back" id="addModal">
            <div class="modal-box">

                <div class="modal-head">
                    <h3>Add User</h3>
                </div>

                <div class="form-field">
                    <label>Full name</label>
                    <asp:TextBox ID="txtAddName" runat="server" CssClass="input" placeholder="e.g. Jordan Lee"></asp:TextBox>
                </div>

                <div class="form-field">
                    <label>Email address</label>
                    <asp:TextBox ID="txtAddEmail" runat="server" CssClass="input" placeholder="you@gmail.com"></asp:TextBox>
                </div>

                <div class="form-field">
                    <label>Password (at least 8 characters)</label>
                    <!-- Password box + eye button on the right -->
                    <div class="password-wrap">
                        <asp:TextBox ID="txtAddPassword" runat="server" ClientIDMode="Static" TextMode="Password" CssClass="input"></asp:TextBox>

                        <button type="button" class="eye-btn" onclick="togglePassword()" title="Show / hide password">
                            <!-- eye with a line through it (password hidden: click to show it) -->
                            <svg id="eyeClosed" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                <path d="M17.94 17.94A10.07 10.07 0 0112 20c-7 0-11-8-11-8a18.45 18.45 0 015.06-5.94M9.9 4.24A9.12 9.12 0 0112 4c7 0 11 8 11 8a18.5 18.5 0 01-2.16 3.19m-6.72-1.07a3 3 0 11-4.24-4.24" />
                                <line x1="1" y1="1" x2="23" y2="23" />
                            </svg>
                            <!-- eye (password showing: click to hide it) -->
                            <svg id="eyeOpen" style="display:none;" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" /><circle cx="12" cy="12" r="3" />
                            </svg>
                        </button>
                    </div>
                </div>

                <div class="form-field">
                    <label>Date of birth (day/month/year)</label>
                    <div class="password-wrap">
                        <asp:TextBox ID="txtAddDob" runat="server" ClientIDMode="Static" CssClass="input" MaxLength="10" placeholder="dd/mm/yyyy" oninput="maskDate(this)"></asp:TextBox>

                        <button type="button" class="eye-btn" onclick="openCalendar('txtAddDob', 'addDobPicker')" title="Pick a date">
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="4" width="18" height="18" rx="2" /><line x1="16" y1="2" x2="16" y2="6" /><line x1="8" y1="2" x2="8" y2="6" /><line x1="3" y1="10" x2="21" y2="10" /></svg>
                        </button>

                        <input type="date" id="addDobPicker" class="hidden-date" tabindex="-1" onchange="pickedDate('addDobPicker', 'txtAddDob')" />
                    </div>
                </div>

                <div class="form-field">
                    <label>Gender</label>
                    <asp:DropDownList ID="ddlAddGender" runat="server" CssClass="input">
                        <asp:ListItem Text="Male" Value="Male"></asp:ListItem>
                        <asp:ListItem Text="Female" Value="Female"></asp:ListItem>
                    </asp:DropDownList>
                </div>

                <div class="form-field">
                    <label>Role</label>
                    <asp:DropDownList ID="ddlAddRole" runat="server" CssClass="input">
                        <asp:ListItem Text="Student" Value="Student"></asp:ListItem>
                        <asp:ListItem Text="Tutor" Value="Tutor"></asp:ListItem>
                        <asp:ListItem Text="Administrator" Value="Admin"></asp:ListItem>
                    </asp:DropDownList>
                </div>

                <div class="form-error">
                    <asp:Label ID="lblAddError" runat="server"></asp:Label>
                </div>

                <div class="modal-buttons">
                    <button type="button" class="btn" onclick="closeModal('addModal')">Cancel</button>
                    <asp:Button ID="btnAddUser" runat="server" Text="Add User" CssClass="btn btn-primary" OnClick="btnAddUser_Click" />
                </div>

            </div>
        </div>


        <!-- ==================================================
             POPUP 4 : DELETE USER (asks "are you sure?")
             ================================================== -->
        <div class="modal-back" id="deleteModal">
            <div class="modal-box danger">

                <div class="modal-head">
                    <h3>Delete User</h3>
                </div>

                <asp:HiddenField ID="hfDeleteId" runat="server" ClientIDMode="Static" />

                <div class="modal-title">Are you sure you want to delete <span id="delName"></span>?</div>
                <p class="modal-text">This user will be permanently removed from the database. This cannot be undone.</p>

                <div class="modal-buttons">
                    <button type="button" class="btn" onclick="closeModal('deleteModal')">Cancel</button>
                    <asp:Button ID="btnConfirmDelete" runat="server" Text="Confirm" CssClass="btn btn-danger" OnClick="btnConfirmDelete_Click" />
                </div>

            </div>
        </div>

    </form>

    <!-- ==================================================
         JAVASCRIPT : the password eye
         (the date of birth helpers are in the <head> above)
         ================================================== -->
    <script type="text/javascript">

        // Eye button: show or hide the password the admin is typing
        function togglePassword() {
            var box = document.getElementById('txtAddPassword');
            var isShowing = (box.type == 'text');

            box.type = isShowing ? 'password' : 'text';                                // switch the box
            // line through the eye = hidden (click to show),  open eye = showing (click to hide)
            document.getElementById('eyeClosed').style.display = isShowing ? 'block' : 'none';
            document.getElementById('eyeOpen').style.display   = isShowing ? 'none' : 'block';
        }
    </script>
</body>
</html>
