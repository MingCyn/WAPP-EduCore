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
    </style>

    <!-- ==================================================
         JAVASCRIPT : controls the popups
         ================================================== -->
    <script type="text/javascript">
        // @ts-nocheck

        var currentId = 0;   // the course we clicked on

        // ----- Show / hide a popup -----
        function openModal(id)  { document.getElementById(id).classList.add('open'); }
        function closeModal(id) { document.getElementById(id).classList.remove('open'); }

        // ----- Helpers -----
        function rowOf(id)       { return document.getElementById('row' + id); }                   // the table row of a course
        function show(id, text)  { document.getElementById(id).innerText = text; }                 // put text in a box
        function orDash(text)    { return (text == null || text == '') ? '—' : text; }             // empty -> a dash

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

        // ----- POPUP 1 : course detail (when you click a row) -----
        function openDetail(id) {
            currentId = id;
            var row = rowOf(id);

            show('dTitle',    row.getAttribute('data-title'));
            show('dTopic',    orDash(row.getAttribute('data-topic')));
            show('dDuration', orDash(row.getAttribute('data-duration')));
            show('dCreated',  row.getAttribute('data-created'));
            show('dId',       id);

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

            // The description box only exists when the table has a description column
            var box = document.getElementById('txtEditDescription');
            if (box != null) { box.value = row.getAttribute('data-description'); }

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
             Same look as the Figma "course preview" card.
             ================================================== -->
        <div class="modal-back" id="detailModal">
            <div class="modal-box">

                <div class="modal-head">
                    <h3>Course preview</h3>
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
                        <button type="button" class="btn btn-primary" onclick="editFromDetail()">Edit course</button>
                        <button type="button" class="btn" onclick="closeModal('detailModal')">Close</button>
                    </div>
                </div>

                <div class="info-box">
                    <div class="info-label">Topic</div>
                    <div class="info-value" id="dTopic"></div>

                    <div class="info-label">Duration</div>
                    <div class="info-value" id="dDuration"></div>

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
                    <h3>Edit course</h3>
                </div>

                <asp:HiddenField ID="hfEditId" runat="server" ClientIDMode="Static" />

                <div class="form-field">
                    <label>Course title</label>
                    <asp:TextBox ID="txtEditTitle" runat="server" ClientIDMode="Static" CssClass="input"></asp:TextBox>
                </div>

                <!-- Only shown when the table has a description column -->
                <asp:Panel ID="pnlEditDesc" runat="server" CssClass="form-field">
                    <label>Description</label>
                    <asp:TextBox ID="txtEditDescription" runat="server" ClientIDMode="Static" TextMode="MultiLine" Rows="3" CssClass="input textarea"></asp:TextBox>
                </asp:Panel>

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
             Same look as the Figma "course preview" card:
             picture on top, then title, description and buttons.
             ================================================== -->
        <div class="modal-back" id="addModal">
            <div class="modal-box">

                <div class="modal-head">
                    <h3>Create course</h3>
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
                        <label>Course title</label>
                        <asp:TextBox ID="txtAddTitle" runat="server" CssClass="input" placeholder="e.g. Applied Data Literacy"></asp:TextBox>
                    </div>

                    <!-- Only shown when the table has a description column -->
                    <asp:Panel ID="pnlAddDesc" runat="server" CssClass="form-field">
                        <label>Description</label>
                        <asp:TextBox ID="txtAddDescription" runat="server" TextMode="MultiLine" Rows="3" CssClass="input textarea" placeholder="A short introduction to the course"></asp:TextBox>
                    </asp:Panel>

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
                        <asp:Button ID="btnAddCourse" runat="server" Text="Create course" CssClass="btn btn-primary" OnClick="btnAddCourse_Click" />
                        <button type="button" class="btn" onclick="closeModal('addModal')">Cancel</button>
                    </div>
                </div>

            </div>
        </div>

    </form>
</body>
</html>
