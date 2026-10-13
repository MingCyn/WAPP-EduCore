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

        /* The course picture in the detail popup (hidden when the course has no picture) */
        .course-img {
            display: none;
            width: 100%;
            max-height: 170px;
            margin-bottom: 14px;
            object-fit: cover;
            border-radius: 10px;
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

        // ----- POPUP 1 : course detail (when you click a row) -----
        function openDetail(id) {
            currentId = id;
            var row = rowOf(id);

            show('dTitle',    row.getAttribute('data-title'));
            show('dTopic',    orDash(row.getAttribute('data-topic')));
            show('dDuration', orDash(row.getAttribute('data-duration')));
            show('dLessons',  row.getAttribute('data-lessons'));
            show('dCreated',  row.getAttribute('data-created'));
            show('dId',       id);

            // Show the picture only if the course has one
            var picture = document.getElementById('dImg');
            var link = row.getAttribute('data-img');
            if (link != '') {
                picture.src = link;
                picture.style.display = 'block';
            } else {
                picture.style.display = 'none';
            }

            openModal('detailModal');
        }

        // The "Edit course" button inside the detail popup
        function editFromDetail() {
            closeModal('detailModal');
            openEdit(currentId);
        }

        // ----- POPUP 2 : edit course -----
        function openEdit(id) {
            var row = rowOf(id);
            document.getElementById('hfEditId').value        = id;
            document.getElementById('txtEditTitle').value    = row.getAttribute('data-title');
            document.getElementById('txtEditTopic').value    = row.getAttribute('data-topic');
            document.getElementById('txtEditDuration').value = row.getAttribute('data-duration');
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
            <a class="menu-item" href="userManagement.aspx">Users</a>
            <a class="menu-item active" href="coursesManagement.aspx">Courses</a>
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
                    <h1>Course Management</h1>
                    <p>Review and maintain the learning catalogue</p>
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
                        <div class="stat-label">All courses</div>
                        <div class="stat-number"><asp:Label ID="lblTotal" runat="server"></asp:Label></div>
                        <div class="stat-note"><asp:Label ID="lblTotalNote" runat="server"></asp:Label></div>
                    </div>

                    <div class="card">
                        <div class="stat-label">Topics</div>
                        <div class="stat-number"><asp:Label ID="lblTopics" runat="server"></asp:Label></div>
                        <div class="stat-note">Different categories</div>
                    </div>

                    <div class="card">
                        <div class="stat-label">Total lessons</div>
                        <div class="stat-number"><asp:Label ID="lblLessons" runat="server"></asp:Label></div>
                        <div class="stat-note">Across all courses</div>
                    </div>

                    <div class="card">
                        <div class="stat-label">Added this month</div>
                        <div class="stat-number"><asp:Label ID="lblNew" runat="server"></asp:Label></div>
                        <div class="stat-note">New courses</div>
                    </div>

                    <div class="card">
                        <div class="stat-label">Without lessons</div>
                        <div class="stat-number"><asp:Label ID="lblNoLessons" runat="server"></asp:Label></div>
                        <div class="stat-note">Need new lessons</div>
                    </div>

                </div>


                <!-- ==============================================
                     COURSE TABLE CARD
                     ============================================== -->
                <div class="card flush">

                    <!-- Search + filters + Create course button.
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
                        <button type="button" class="btn btn-primary" onclick="openModal('addModal')">+ Create course</button>

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
             ================================================== -->
        <div class="modal-back" id="detailModal">
            <div class="modal-box">

                <div class="modal-head">
                    <h3>Course detail</h3>
                </div>

                <img id="dImg" class="course-img" alt="Course picture" />

                <div class="modal-title" id="dTitle"></div>
                <p class="modal-text">Information saved for this course.</p>

                <div class="info-box">
                    <div class="info-label">Topic</div>
                    <div class="info-value" id="dTopic"></div>

                    <div class="info-label">Duration</div>
                    <div class="info-value" id="dDuration"></div>

                    <div class="info-label">Lessons</div>
                    <div class="info-value" id="dLessons"></div>

                    <div class="info-label">Created</div>
                    <div class="info-value" id="dCreated"></div>

                    <div class="info-label">Course ID</div>
                    <div class="info-value" id="dId"></div>
                </div>

                <div class="modal-buttons">
                    <button type="button" class="btn" onclick="editFromDetail()">Edit course</button>
                    <button type="button" class="btn" onclick="closeModal('detailModal')">Close</button>
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
                    <h3>Edit course</h3>
                </div>

                <asp:HiddenField ID="hfEditId" runat="server" ClientIDMode="Static" />

                <div class="form-field">
                    <label>Course title</label>
                    <asp:TextBox ID="txtEditTitle" runat="server" ClientIDMode="Static" CssClass="input"></asp:TextBox>
                </div>

                <div class="form-field">
                    <label>Topic</label>
                    <asp:TextBox ID="txtEditTopic" runat="server" ClientIDMode="Static" CssClass="input" list="topicList"></asp:TextBox>
                </div>

                <div class="form-field">
                    <label>Duration</label>
                    <asp:TextBox ID="txtEditDuration" runat="server" ClientIDMode="Static" CssClass="input"></asp:TextBox>
                </div>

                <div class="modal-buttons">
                    <button type="button" class="btn" onclick="closeModal('editModal')">Cancel</button>
                    <asp:Button ID="btnSaveEdit" runat="server" Text="Save changes" CssClass="btn btn-primary" OnClick="btnSaveEdit_Click" />
                </div>

            </div>
        </div>


        <!-- ==================================================
             POPUP 3 : CREATE COURSE
             ================================================== -->
        <div class="modal-back" id="addModal">
            <div class="modal-box">

                <div class="modal-head">
                    <h3>Create course</h3>
                </div>

                <div class="form-field">
                    <label>Course title</label>
                    <asp:TextBox ID="txtAddTitle" runat="server" CssClass="input" placeholder="e.g. Introduction to Python"></asp:TextBox>
                </div>

                <div class="form-field">
                    <label>Topic</label>
                    <asp:TextBox ID="txtAddTopic" runat="server" CssClass="input" list="topicList" placeholder="Pick one or type a new topic"></asp:TextBox>
                </div>

                <div class="form-field">
                    <label>Duration</label>
                    <asp:TextBox ID="txtAddDuration" runat="server" CssClass="input" placeholder="e.g. 4"></asp:TextBox>
                </div>

                <div class="form-error">
                    <asp:Label ID="lblAddError" runat="server"></asp:Label>
                </div>

                <div class="modal-buttons">
                    <button type="button" class="btn" onclick="closeModal('addModal')">Cancel</button>
                    <asp:Button ID="btnAddCourse" runat="server" Text="Create course" CssClass="btn btn-primary" OnClick="btnAddCourse_Click" />
                </div>

            </div>
        </div>

    </form>
</body>
</html>
