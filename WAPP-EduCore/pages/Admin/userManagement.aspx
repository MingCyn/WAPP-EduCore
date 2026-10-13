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

        // The "Edit user" button inside the detail popup
        function editFromDetail() {
            closeModal('detailModal');
            openEdit(currentId);
        }

        // ----- POPUP 2 : edit user -----
        function openEdit(id) {
            var row = rowOf(id);
            document.getElementById('hfEditId').value     = id;
            document.getElementById('txtEditName').value  = row.getAttribute('data-name');
            document.getElementById('txtEditEmail').value = row.getAttribute('data-email');
            document.getElementById('txtEditDob').value   = row.getAttribute('data-dobraw');   // looks like 2012-05-30
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

            <a class="menu-item" href="adminDashboard.aspx">Dashboard</a>
            <a class="menu-item active" href="userManagement.aspx">Users</a>
            <a class="menu-item" href="coursesManagement.aspx">Courses</a>
            <a class="menu-item" href="adminReports.aspx">Reports</a>

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
                <div>
                    <span class="avatar"><%= AdminInitials %></span>
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
                        <div class="stat-label">Total users</div>
                        <div class="stat-number"><asp:Label ID="lblTotal" runat="server"></asp:Label></div>
                        <div class="stat-note">All registered accounts</div>
                    </div>

                    <div class="card">
                        <div class="stat-label">New this month</div>
                        <div class="stat-number"><asp:Label ID="lblNew" runat="server"></asp:Label></div>
                        <div class="stat-note">Joined this month</div>
                    </div>

                    <div class="card">
                        <div class="stat-label">Students</div>
                        <div class="stat-number"><asp:Label ID="lblStudents" runat="server"></asp:Label></div>
                        <div class="stat-note"><asp:Label ID="lblStudentsNote" runat="server"></asp:Label></div>
                    </div>

                    <div class="card">
                        <div class="stat-label">Tutors</div>
                        <div class="stat-number"><asp:Label ID="lblTutors" runat="server"></asp:Label></div>
                        <div class="stat-note"><asp:Label ID="lblTutorsNote" runat="server"></asp:Label></div>
                    </div>

                    <div class="card">
                        <div class="stat-label">Administrators</div>
                        <div class="stat-number"><asp:Label ID="lblAdmins" runat="server"></asp:Label></div>
                        <div class="stat-note"><asp:Label ID="lblAdminsNote" runat="server"></asp:Label></div>
                    </div>

                </div>


                <!-- ==============================================
                     USER TABLE CARD
                     ============================================== -->
                <div class="card flush">

                    <!-- Search + filter + Add user button.
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
                        <button type="button" class="btn btn-primary" onclick="openModal('addModal')">+ Add user</button>

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
                    <button type="button" class="btn" onclick="editFromDetail()">Edit user</button>
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
                    <h3>Edit user</h3>
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
                    <label>Date of birth</label>
                    <asp:TextBox ID="txtEditDob" runat="server" ClientIDMode="Static" TextMode="Date" CssClass="input"></asp:TextBox>
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
                    <h3>Add user</h3>
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
                            <!-- eye (password hidden) -->
                            <svg id="eyeOpen" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" /><circle cx="12" cy="12" r="3" />
                            </svg>
                            <!-- eye with a line through it (password showing) -->
                            <svg id="eyeClosed" style="display:none;" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                <path d="M17.94 17.94A10.07 10.07 0 0112 20c-7 0-11-8-11-8a18.45 18.45 0 015.06-5.94M9.9 4.24A9.12 9.12 0 0112 4c7 0 11 8 11 8a18.5 18.5 0 01-2.16 3.19m-6.72-1.07a3 3 0 11-4.24-4.24" />
                                <line x1="1" y1="1" x2="23" y2="23" />
                            </svg>
                        </button>
                    </div>
                </div>

                <div class="form-field">
                    <label>Date of birth</label>
                    <asp:TextBox ID="txtAddDob" runat="server" TextMode="Date" CssClass="input"></asp:TextBox>
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
                    <asp:Button ID="btnAddUser" runat="server" Text="Add user" CssClass="btn btn-primary" OnClick="btnAddUser_Click" />
                </div>

            </div>
        </div>

    </form>

    <!-- ==================================================
         JAVASCRIPT : fixes the calendar (date of birth)
         Click the little calendar icon  -> it opens
         Click the icon again            -> it closes
         Click the icon one more time    -> it opens again
         ================================================== -->
    <script type="text/javascript">

        function fixDatePicker(box) {
            var isOpen = false;     // is the calendar showing right now?
            var wasOpen = false;    // was it showing when the mouse was pressed?

            // Did the click land on the little calendar icon? (it sits at the right end of the box)
            function onIcon(e) {
                var rect = box.getBoundingClientRect();
                return e.clientX > rect.right - 34;
            }

            // Mouse pressed: remember if the calendar was open
            box.addEventListener('mousedown', function () {
                wasOpen = isOpen;
            });

            // Mouse clicked
            box.addEventListener('click', function (e) {
                if (!onIcon(e)) { return; }

                if (wasOpen) {
                    e.preventDefault();   // the press already closed it, so do NOT open it again
                    isOpen = false;
                } else {
                    isOpen = true;        // the browser opens it
                }
                wasOpen = false;
            });

            // The calendar is closed when: a date is picked, the box loses focus, or Esc is pressed
            box.addEventListener('change', function () { isOpen = false; });
            box.addEventListener('blur', function () { isOpen = false; });
            box.addEventListener('keydown', function (e) {
                if (e.key == 'Escape') { isOpen = false; }
            });
        }

        // Eye button: show or hide the password the admin is typing
        function togglePassword() {
            var box = document.getElementById('txtAddPassword');
            var isShowing = (box.type == 'text');

            box.type = isShowing ? 'password' : 'text';                                // switch the box
            document.getElementById('eyeOpen').style.display   = isShowing ? 'block' : 'none';
            document.getElementById('eyeClosed').style.display = isShowing ? 'none' : 'block';
        }

        // Use the fix on EVERY date box on this page (Edit popup + Add user popup)
        var dateBoxes = document.querySelectorAll('input[type="date"]');
        for (var i = 0; i < dateBoxes.length; i++) {
            fixDatePicker(dateBoxes[i]);
        }
    </script>
</body>
</html>
