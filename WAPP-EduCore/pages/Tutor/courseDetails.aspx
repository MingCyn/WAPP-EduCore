<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="courseDetails.aspx.cs" Inherits="WAPP_EduCore.pages.Tutor.courseDetails" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>EduCore - Course Details</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Source+Serif+4:wght@600;700&display=swap" rel="stylesheet" />
    <link href="../../Content/dashboard.css?v=3" rel="stylesheet" type="text/css" />
    <%-- courseBuilder.css supplies the shared .btn, pop-up and form styles --%>
    <link href="../../Content/courseBuilder.css?v=4" rel="stylesheet" type="text/css" />
    <link href="../../Content/courseDetails.css?v=2" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
        <div class="dashboard-layout">

            <%-- Shared nav: same include as the other tutor pages --%>
            <% Server.Execute("tutorNav.aspx", false); %>

            <main class="dashboard-container">

                <!-- Breadcrumb -->
                <nav class="cd-crumb" aria-label="Breadcrumb">
                    <a href="courses.aspx">My Courses</a> &gt; <asp:Literal ID="LitCrumbTitle" runat="server" />
                </nav>

                <!-- Title, status and actions -->
                <div class="cd-head">
                    <div>
                        <h1 class="cd-title"><asp:Literal ID="LitTitle" runat="server" /></h1>
                        <div class="cd-meta">
                            <asp:Label ID="LblStatusPill" runat="server" CssClass="cd-pill draft" Text="Draft" />
                            <span><asp:Literal ID="LitMeta" runat="server" /></span>
                        </div>
                    </div>
                    <div class="cd-actions">
                        <button type="button" class="btn btn-outline" onclick="openModal('modalCourseInfo')">Edit Course</button>
                        <a class="btn btn-outline" href='courseAnalysis.aspx?courseId=<%= CourseIdValue %>'>View Analytics</a>
                    </div>
                </div>

                <asp:Label ID="LblStatus" runat="server" CssClass="page-status" Visible="false" />

                <%-- Shown only while the course is published --%>
                <asp:Panel ID="PnlLiveNotice" runat="server" CssClass="notice warning cd-notice" Visible="false">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="9" /><path d="M12 8v5" /><circle cx="12" cy="16.5" r=".6" fill="currentColor" /></svg>
                    <span>This course is <strong>published</strong>. Changes you save here are visible to students straight away.</span>
                </asp:Panel>

                <div class="cd-layout">

                    <!-- ================= LEFT: overview + syllabus ================= -->
                    <div>

                        <!-- Course overview -->
                        <section class="cd-card cd-overview">
                            <div class="cd-thumb"><asp:Literal ID="LitThumb" runat="server" /></div>
                            <div class="cd-overview-body">
                                <div class="cd-card-head">
                                    <h2 class="cd-card-title">Course overview</h2>
                                </div>
                                <p class="cd-description"><asp:Literal ID="LitDescription" runat="server" /></p>
                                <div class="cd-created">Created by <asp:Literal ID="LitCreatedBy" runat="server" /></div>
                            </div>
                        </section>

                        <!-- Course syllabus (the lessons of this course) -->
                        <div class="cd-section-head">
                            <h2 class="cd-section-title">Course syllabus</h2>
                            <a class="btn btn-outline btn-sm cd-add" href='courseBuilder.aspx?courseId=<%= CourseIdValue %>&amp;addLesson=1'>
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round"><path d="M12 5v14M5 12h14" /></svg>
                                Add Lesson
                            </a>
                        </div>

                        <asp:Repeater ID="RptLessons" runat="server">
                            <ItemTemplate>
                                <div class="cd-lesson">
                                    <div class="cd-lesson-row">
                                        <span class="cd-lesson-icon"><%# Eval("IconHtml") %></span>
                                        <div class="cd-lesson-main">
                                            <div class="cd-lesson-title"><%#: Eval("Number") %> <%#: Eval("Title") %></div>
                                            <div class="cd-lesson-sub"><%#: Eval("Subtitle") %></div>
                                        </div>
                                        <a class="cd-link" href='<%#: Eval("EditUrl") %>'>
                                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4 12.5-12.5z" /></svg>
                                            Edit
                                        </a>
                                    </div>

                                    <%-- The video / interactive object / assessment / lab of a mixed-content lesson --%>
                                    <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("ShowComponents") %>'>
                                        <div class="cd-components">
                                            <asp:Repeater runat="server" DataSource='<%# Eval("Components") %>'>
                                                <ItemTemplate>
                                                    <div class="cd-component">
                                                        <span class="cd-component-icon"><%# Eval("IconHtml") %></span>
                                                        <span class="cd-component-title"><%#: Eval("Title") %></span>
                                                        <span class="cd-component-meta"><%# (bool)Eval("IsDraft") ? "<strong class='cd-draft'>Draft</strong>" : "" %><%#: Eval("Meta") %></span>
                                                    </div>
                                                </ItemTemplate>
                                            </asp:Repeater>
                                        </div>
                                    </asp:PlaceHolder>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>

                        <asp:Panel ID="PnlNoLessons" runat="server" CssClass="cd-empty" Visible="false">
                            This course has no lessons yet. Use <strong>Add Lesson</strong> to create the first one.
                        </asp:Panel>
                    </div>

                    <!-- ================= RIGHT: publishing, outcomes, references ================= -->
                    <aside class="cd-side">

                        <!-- Publishing overview -->
                        <section class="cd-card cd-publish">
                            <h2 class="cd-card-title">Publishing overview</h2>
                            <asp:Label ID="LblPublishPill" runat="server" CssClass="cd-pill draft" Text="Draft course" />
                            <p class="cd-publish-text">
                                <asp:Literal ID="LitPublishText" runat="server" /></p>
                            <div class="cd-stats">
                                <div>
                                    <span class="cd-stat-num">
                                        <asp:Literal ID="LitEnrolled" runat="server" /></span>
                                    <span class="cd-stat-label">Enrolled students</span>
                                </div>
                                <div>
                                    <span class="cd-stat-num">
                                        <asp:Literal ID="LitLessonCount" runat="server" /></span>
                                    <span class="cd-stat-label">Lessons</span>
                                </div>
                            </div>
                        </section>
                        <!-- Learning outcomes -->
                        <section class="cd-card">
                            <div class="cd-card-head">
                                <h2 class="cd-card-title">Learning outcomes</h2>
                                <button type="button" class="btn btn-outline btn-sm cd-add" onclick="openCleanModal('modalOutcome', ['TxtOutcome'], 'outcomeError')">
                                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round">
                                        <path d="M12 5v14M5 12h14" />
                                    </svg>
                                    Add
                                </button>
                            </div>
                            <p class="cd-card-sub">What your students will learn</p>

                            <asp:Panel ID="PnlOutcomeList" runat="server" Visible="false">
                                <ul class="cd-list">
                                    <asp:Repeater ID="RptOutcomes" runat="server" OnItemCommand="RptOutcomes_ItemCommand">
                                        <ItemTemplate>
                                            <li class="cd-item">
                                                <span class="cd-item-icon">
                                                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                                                        <circle cx="12" cy="12" r="9" />
                                                        <path d="M8 12.5l2.7 2.7L16 9.5" />
                                                    </svg></span>
                                                <span class="cd-item-text"><%#: Eval("Text") %></span>
                                                <asp:LinkButton ID="BtnDeleteOutcome" runat="server" CssClass="cd-remove" CausesValidation="false" ToolTip="Remove" OnClientClick='<%# "showDeleteModal(" + Eval("Id") + ", \"outcome\"); return false;" %>'>
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="16" height="16"><path d="M18 6L6 18M6 6l12 12"/></svg>
                                                </asp:LinkButton>
                                            </li>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </ul>
                            </asp:Panel>
                            <asp:Panel ID="PnlNoOutcomes" runat="server" CssClass="cd-empty" Visible="false">
                                No learning outcomes yet. Add what students will be able to do after this course.
                            </asp:Panel>
                        </section>

                        <!-- References -->
                        <section class="cd-card">
                            <div class="cd-card-head">
                                <h2 class="cd-card-title">References</h2>
                                <button type="button" class="btn btn-outline btn-sm cd-add" onclick="openCleanModal('modalReference', ['TxtRefTitle'], 'referenceError')">
                                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round">
                                        <path d="M12 5v14M5 12h14" />
                                    </svg>
                                    Add
                                </button>
                            </div>
                            <p class="cd-card-sub">Books, papers and links students can read</p>

                            <asp:Panel ID="PnlReferenceList" runat="server" Visible="false">
                                <ul class="cd-list">
                                    <asp:Repeater ID="RptReferences" runat="server" OnItemCommand="RptReferences_ItemCommand">
                                        <ItemTemplate>
                                            <li class="cd-item ref">
                                                <span class="cd-item-icon"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M4 5.5A2.5 2.5 0 0 1 6.5 3H20v16H6.5A2.5 2.5 0 0 0 4 21.5v-16z" /><path d="M4 21.5A2.5 2.5 0 0 1 6.5 19H20" /></svg></span>
                                                <span class="cd-item-text">
                                                    <%# Eval("TitleHtml") %>
                                                    <span class="cd-item-host"><%#: Eval("HostText") %></span>
                                                </span>
                                                <asp:LinkButton runat="server" CommandName="delete" CommandArgument='<%# Eval("Id") %>' CssClass="cd-remove" CausesValidation="false" ToolTip="Remove" OnClientClick='<%# "showDeleteModal(" + Eval("Id") + ", \"reference\"); return false;" %>'>
                                                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M18 6L6 18M6 6l12 12" /></svg>
                                                </asp:LinkButton>
                                            </li>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </ul>
                            </asp:Panel>
                            <asp:Panel ID="PnlNoReferences" runat="server" CssClass="cd-empty" Visible="false">
                                No references yet. Add books, papers or links that support this course.
                            </asp:Panel>
                        </section>

                    </aside>
                </div>
            </main>
        </div>

        <!-- ============ POP-UP DIALOGS ============ -->
        <%-- Pressing Enter in a pop-up never saves: only the pop-up's own button does. --%>

        <%-- Add a learning outcome --%>
        <div class="modal-overlay" id="modalOutcome" aria-hidden="true">
            <div class="modal" role="dialog" aria-modal="true" aria-labelledby="title_modalOutcome">
                <h3 class="modal-title" id="title_modalOutcome">Add learning outcome</h3>
                <div id="outcomeError" class="page-status error" style="display:none"></div>
                <div class="form-group">
                    <label class="form-label" for="TxtOutcome">What will students be able to do?</label>
                    <asp:TextBox ID="TxtOutcome" runat="server" CssClass="form-control" MaxLength="300" placeholder="e.g. Analyze algorithm efficiency with Big O notation." />
                    <p class="form-hint">Start with an action, for example Explain, Compare, Build or Solve. Up to 300 characters.</p>
                </div>
                <div class="modal-actions">
                    <button type="button" class="btn btn-cancel" onclick="closeModal('modalOutcome')">Cancel</button>
                    <asp:Button ID="BtnSaveOutcome" runat="server" OnClick="BtnSaveOutcome_Click" Text="Add outcome" CssClass="btn btn-accent" />
                </div>
            </div>
        </div>

        <%-- Add a reference --%>
        <div class="modal-overlay" id="modalReference" aria-hidden="true">
            <div class="modal" role="dialog" aria-modal="true" aria-labelledby="title_modalReference">
                <h3 class="modal-title" id="title_modalReference">Add reference</h3>
                <div id="referenceError" class="page-status error" style="display:none"></div>
                <div class="form-group">
                    <label class="form-label" for="TxtRefTitle">Title</label>
                    <asp:TextBox ID="TxtRefTitle" runat="server" CssClass="form-control" MaxLength="200" placeholder="e.g. Introduction to Algorithms, 4th edition" />
                </div>
                <div class="modal-actions">
                    <button type="button" class="btn btn-cancel" onclick="closeModal('modalReference')">Cancel</button>
                    <asp:Button ID="BtnSaveReference" runat="server" OnClick="BtnSaveReference_Click" Text="Add reference" CssClass="btn btn-accent" />
                </div>
            </div>
        </div>

        <!-- Edit course info modal matching courseBuilder.aspx -->
        <div class="modal-overlay" id="modalCourseInfo" aria-hidden="true">
            <asp:Panel ID="PnlCourseInfo" runat="server" CssClass="modal" role="dialog" aria-modal="true">
                <h3 class="modal-title">Edit course information</h3>
                <div id="courseInfoError" class="page-status error" style="display: none"></div>

                <div class="form-group">
                    <label class="form-label" for="TxtCourseTitle">Course title</label>
                    <asp:TextBox ID="TxtCourseTitle" runat="server" CssClass="form-control" MaxLength="150" />
                </div>
                <div class="form-group">
                    <label class="form-label" for="TxtCourseTopic">Topic</label>
                    <asp:TextBox ID="TxtCourseTopic" runat="server" CssClass="form-control" MaxLength="80" />
                </div>
                <div class="form-group">
                    <label class="form-label" for="DdlCourseLevel">Course level</label>
                    <asp:DropDownList ID="DdlCourseLevel" runat="server" CssClass="form-control">
                        <asp:ListItem Value="" Text="Select level" />
                        <asp:ListItem Value="IGCSE" Text="IGCSE" />
                        <asp:ListItem Value="SPM" Text="SPM" />
                    </asp:DropDownList>
                </div>

                <div class="form-row">
                    <div class="form-group">
                        <label class="form-label" for="DdlCourseWeeks">Duration (weeks)</label>
                        <asp:DropDownList ID="DdlCourseWeeks" runat="server" CssClass="form-control">
                            <asp:ListItem Value="" Text="Select weeks" />
                        </asp:DropDownList>
                    </div>
                    <div class="form-group">
                        <label class="form-label" for="DdlCourseHours">Hours per week</label>
                        <asp:DropDownList ID="DdlCourseHours" runat="server" CssClass="form-control">
                            <asp:ListItem Value="" Text="Select hours" />
                        </asp:DropDownList>
                    </div>
                </div>

                <div class="form-group">
                    <!-- Course Image Section inside your Modal -->
                    <label>Course image</label>
                    <div style="margin-bottom: 12px;">
                        <!-- Current Image Row with 'X' aligned to the right -->
                        <asp:Panel ID="PnlCurrentImg" runat="server" CssClass="current-img-row" Style="display: flex; align-items: center; justify-content: space-between; background: #f8fafc; padding: 8px 12px; border: 1px solid #e2e8f0; border-radius: 6px; margin-bottom: 8px;">
                            <div style="display: flex; align-items: center; gap: 10px;">
                                <asp:Literal ID="LitCurrentImgPreview" runat="server" />
                                <span style="font-size: 13px; color: #475569; font-weight: 500;">Current image</span>
                            </div>
                            <asp:LinkButton ID="BtnRemoveImg" runat="server" CssClass="btn-remove-badge" OnClick="BtnRemoveImg_Click" ToolTip="Remove current image" style="background: #dc2626; color: white; border-radius: 50%; width: 24px; height: 24px; display: inline-flex; align-items: center; justify-content: center; text-decoration: none; font-size: 13px; font-weight: bold;">&times;</asp:LinkButton>
                        </asp:Panel>
                        <asp:HiddenField ID="HfRemoveImage" runat="server" Value="false" />
                    </div>

                    <!-- Upload Box with Live Preview Support -->
                    <div class="dropzone-container" style="border: 2px dashed #cbd5e1; padding: 16px; text-align: center; border-radius: 8px; background: #ffffff;">
                        <asp:FileUpload ID="FileCourseImg" runat="server" CssClass="form-control-file" onchange="previewUploadedImage(this);" />
                        <small class="form-text text-muted" style="display: block; margin-top: 6px;">JPG or PNG, up to 2 MB (Select 1 image)</small>

                        <!-- Live Preview Container for newly picked file -->
                        <div id="newImagePreviewContainer" style="display: none; margin-top: 10px; align-items: center; justify-content: center; gap: 8px;">
                            <img id="imgPreviewTag" src="" alt="New preview" style="max-height: 60px; border-radius: 4px; border: 1px solid #cbd5e1;" />
                            <span id="lblFileName" style="font-size: 12px; color: #1e293b;"></span>
                        </div>
                    </div>

                <div class="modal-actions">
                    <button type="button" class="btn btn-cancel" onclick="closeModal('modalCourseInfo')">Cancel</button>
                    <asp:Button ID="BtnSaveCourseInfo" runat="server" Text="Save changes" CssClass="btn btn-accent" OnClick="BtnSaveCourseInfo_Click" />
                </div>
            </asp:Panel>
        </div>

        <%-- Custom Confirmation Modal --%>
        <div class="modal-overlay" id="modalConfirmDelete" aria-hidden="true">
            <div class="modal" role="dialog" aria-modal="true" aria-labelledby="title_modalConfirmDelete">
                <h3 class="modal-title" id="title_modalConfirmDelete">Confirm Deletion</h3>
                <p class="modal-desc" id="confirmDeleteMessage" style="margin: 1rem 0; color: var(--text-muted, #64748b);">
                    Are you sure you want to delete this item?
                </p>
                <div class="modal-actions">
                    <button type="button" class="btn btn-cancel" onclick="closeModal('modalConfirmDelete')">Cancel</button>
                    <asp:Button ID="BtnConfirmDelete" runat="server" OnClick="BtnConfirmDelete_Click" Text="Yes, Delete" CssClass="btn btn-danger-custom" />
                </div>
            </div>
        </div>

        <asp:HiddenField ID="HfTargetId" runat="server" />
        <asp:HiddenField ID="HfDeleteType" runat="server" />
    </form>

    <script>
        // @ts-nocheck
        var lastFocus = null;

        function openModal(id) {
            var m = document.getElementById(id);
            if (!m) return;
            lastFocus = document.activeElement;
            m.classList.add('open');
            m.setAttribute('aria-hidden', 'false');
            document.body.classList.add('modal-open');
            var first = m.querySelector('input:not([type=hidden]):not([type=file]), select, textarea');
            if (first) setTimeout(function () { first.focus(); }, 50);
        }

        function closeModal(id) {
            var m = document.getElementById(id);
            if (!m) return;
            m.classList.remove('open');
            m.setAttribute('aria-hidden', 'true');
            if (!document.querySelector('.modal-overlay.open')) document.body.classList.remove('modal-open');
            if (lastFocus) lastFocus.focus();
        }

        function openCleanModal(id, fieldIds, errorId) {
            fieldIds.forEach(function (f) {
                var el = document.getElementById(f);
                if (el) el.value = '';
            });
            var box = document.getElementById(errorId);
            if (box) box.style.display = 'none';
            openModal(id);
        }

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                document.querySelectorAll('.modal-overlay.open').forEach(function (m) { closeModal(m.id); });
            }
        });

        function showDeleteModal(id, type) {
            document.getElementById('<%= HfTargetId.ClientID %>').value = id;
            document.getElementById('<%= HfDeleteType.ClientID %>').value = type;

            var msg = (type === 'outcome') ? 'Do you want to delete this learning outcome?' : 'Do you want to delete this reference?';
            document.getElementById('confirmDeleteMessage').textContent = msg;

            openModal('modalConfirmDelete');
        }

        function removeCurrentImage() {
            var previewPanel = document.getElementById('<%= PnlCurrentImg.ClientID %>');
            var removeField = document.getElementById('<%= HfRemoveImage.ClientID %>');
            if (previewPanel) previewPanel.style.display = 'none';
            if (removeField) removeField.value = 'true';
        }

        function previewUploadedImage(input) {
            var container = document.getElementById('newImagePreviewContainer');
            var imgTag = document.getElementById('imgPreviewTag');
            var nameSpan = document.getElementById('lblFileName');

            if (input.files && input.files[0]) {
                // Check file count enforcement
                if (input.files.length > 1) {
                    alert("Please select only one image.");
                    input.value = "";
                    container.style.display = 'none';
                    return;
                }

                var reader = new FileReader();
                reader.onload = function (e) {
                    imgTag.src = e.target.result;
                    nameSpan.textContent = input.files[0].name;
                    container.style.display = 'flex';
                }
                reader.readAsDataURL(input.files[0]);
            } else {
                container.style.display = 'none';
            }
        }

        document.querySelectorAll('.modal-overlay').forEach(function (o) {
            o.addEventListener('mousedown', function (e) { if (e.target === o) closeModal(o.id); });
        });

        document.addEventListener('keydown', function (e) {
            if (e.key !== 'Enter') return;
            var t = e.target;
            if (!t || !t.closest || !t.closest('.modal')) return;
            var tag = t.tagName;
            if (tag === 'TEXTAREA' || tag === 'BUTTON') return;
            if (tag === 'INPUT' && (t.type === 'submit' || t.type === 'button')) return;
            e.preventDefault();
        });

        var navLink = document.querySelector('.nav-link[href="courses.aspx"]');
        if (navLink) navLink.classList.add('active');
    </script>
</body>
</html>