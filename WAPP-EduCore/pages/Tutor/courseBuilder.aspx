<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="courseBuilder.aspx.cs" Inherits="WAPP_EduCore.pages.Tutor.WebForm1" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>EduCore - Course Builder</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Source+Serif+4:wght@600;700&display=swap" rel="stylesheet" />
    <link href="../../Content/dashboard.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/courseBuilder.css?v=4" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
        <div class="dashboard-layout">

            <%-- Shared nav: use the SAME line you already use in tutorDashboard.aspx.
                 Server.Execute is shown here as a default. --%>
            <% Server.Execute("tutorNav.aspx"); %>

            <main class="dashboard-container">

                <!-- Page header -->
                <div class="page-header">
                    <div>
                        <h1 class="page-title">Course Builder</h1>
                        <p class="page-subtitle">
                            <asp:Label ID="LblCourseName" runat="server" Text="Advanced Data Structures &amp; Algorithms" />
                            <button type="button" class="link-edit" onclick="openModal('modalCourseInfo')">
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 20h9" /><path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4 12.5-12.5z" /></svg>
                                Edit course info
                            </button>
                        </p>
                    </div>
                    <div class="page-actions">
                        <asp:Button ID="BtnPreviewCourse" runat="server" Text="Preview Course" CssClass="btn btn-outline" />
                        <asp:Button ID="BtnPublishCourse" runat="server" Text="Publish Course" CssClass="btn btn-primary" />
                    </div>
                </div>

                <asp:Label ID="LblStatus" runat="server" CssClass="page-status" Visible="false" />

                <div class="builder-layout">

                    <!-- Left: Course Outline -->
                    <aside class="outline-card">
                        <h3 class="outline-title">Course Outline</h3>
                        <p class="outline-hint">Drag items to structure lessons</p>

                        <!-- Lessons of this course (data-bound) -->
                        <div class="module">
                            <div class="module-header">
                                <svg class="chevron" viewBox="0 0 12 12" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M2 4l4 4 4-4" /></svg>
                                <span>Lessons</span>
                            </div>
                            <div class="lesson-list">
                                <asp:Repeater ID="RptLessons" runat="server">
                                    <ItemTemplate>
                                        <a href='<%# GetLessonUrl(Eval("LessonId")) %>' class='lesson-item<%# (bool)Eval("IsActive") ? " active" : "" %>'>
                                            <svg class="drag-handle" viewBox="0 0 10 14" fill="currentColor"><circle cx="2" cy="2" r="1.2" /><circle cx="8" cy="2" r="1.2" /><circle cx="2" cy="7" r="1.2" /><circle cx="8" cy="7" r="1.2" /><circle cx="2" cy="12" r="1.2" /><circle cx="8" cy="12" r="1.2" /></svg>
                                            <%# GetLessonIcon(Eval("HasVideo")) %>
                                            <span class="lesson-name"><%#: Eval("Title") %></span>
                                            <svg class="lesson-menu" viewBox="0 0 14 14" fill="currentColor"><circle cx="2.5" cy="7" r="1.1" /><circle cx="7" cy="7" r="1.1" /><circle cx="11.5" cy="7" r="1.1" /></svg>
                                        </a>
                                    </ItemTemplate>
                                </asp:Repeater>
                                <asp:Panel ID="PnlNoLessons" runat="server" CssClass="outline-empty" Visible="false">This course has no lessons yet.</asp:Panel>
                            </div>
                        </div>

                        <asp:Button ID="BtnAddModule" runat="server" OnClientClick="openModal('modalAddModule'); return false;" Text="+  Add New Module" CssClass="btn btn-muted btn-block" />
                    </aside>

                    <!-- Right: Lesson editor -->
                    <section class="editor-card">
                        <asp:Panel ID="PnlNoLesson" runat="server" CssClass="component-empty" Visible="false">Select a lesson from the outline to start editing.</asp:Panel>
                        <asp:Panel ID="PnlLessonView" runat="server">
                        <div class="editor-header">
                            <div>
                                <h2 class="editor-title"><asp:Literal ID="LitLessonTitle" runat="server" /></h2>
                                <p class="editor-meta"><asp:Literal ID="LitLessonMeta" runat="server" /></p>
                            </div>
                            <asp:Button ID="BtnEditDetails" runat="server" OnClientClick="openModal('modalEditDetails'); return false;" Text="Edit Details" CssClass="btn btn-outline btn-sm" />
                        </div>

                        <asp:Panel ID="PnlSummary" runat="server" Visible="false">
                            <p class="editor-summary"><asp:Literal ID="LitSummary" runat="server" /></p>
                        </asp:Panel>

                        <div class="component-list">

                            <!-- Video (lessons.video_url) -->
                            <asp:Panel ID="PnlVideo" runat="server" CssClass="component-card" Visible="false">
                                <div class="video-thumb"></div>
                                <div class="component-body">
                                    <h3 class="component-title">Lesson Video</h3>
                                    <p class="component-meta">Video Component <span class="dot-sep">&bull;</span> <asp:Literal ID="LitVideoSource" runat="server" /></p>
                                    <div class="component-actions">
                                        <asp:Button ID="BtnUploadVideo" runat="server" OnClientClick="openModal('modalUploadVideo'); return false;" Text="Replace Video" CssClass="btn btn-primary btn-sm" />
                                    </div>
                                </div>
                            </asp:Panel>

                            <!-- Interactive object (block of type "interactive" in lessons.lesson_content) -->
                            <asp:Panel ID="PnlInteractive" runat="server" CssClass="component-card" Visible="false">
                                <div class="component-icon navy">
                                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M5 7l5 5-5 5" /><path d="M13 18h6" /></svg>
                                </div>
                                <div class="component-body">
                                    <h3 class="component-title"><asp:Literal ID="LitInteractiveTitle" runat="server" /></h3>
                                    <p class="component-meta"><asp:Literal ID="LitInteractiveMeta" runat="server" /></p>
                                    <div class="component-actions">
                                        <a id="LnkInteractive" runat="server" class="btn btn-accent btn-sm">Configure Sandbox</a>
                                    </div>
                                </div>
                            </asp:Panel>

                            <!-- Assessment (lessons.quiz_id) -->
                            <asp:Panel ID="PnlAssessment" runat="server" CssClass="component-card" Visible="false">
                                <div class="component-icon indigo">
                                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="9" /><path d="M9.5 9.5a2.5 2.5 0 1 1 3.5 2.3c-.7.3-1 .9-1 1.7" /><circle cx="12" cy="17" r=".6" fill="currentColor" /></svg>
                                </div>
                                <div class="component-body">
                                    <h3 class="component-title">Lesson Assessment</h3>
                                    <p class="component-meta"><asp:Literal ID="LitAssessmentMeta" runat="server" /></p>
                                    <div class="component-actions">
                                        <a id="LnkAssessment" runat="server" class="btn btn-outline btn-sm">Edit Questions</a>
                                    </div>
                                </div>
                            </asp:Panel>

                            <!-- Virtual lab (block of type "lab" in lessons.lesson_content) -->
                            <asp:Panel ID="PnlLab" runat="server" CssClass="component-card" Visible="false">
                                <div class="component-icon amber">
                                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M9 3h6M10 3v6l-5.5 9.5A1.5 1.5 0 0 0 5.8 21h12.4a1.5 1.5 0 0 0 1.3-2.5L14 9V3" /><path d="M7.5 15h9" /></svg>
                                </div>
                                <div class="component-body">
                                    <h3 class="component-title"><asp:Literal ID="LitLabTitle" runat="server" /> <asp:Label ID="LblLabDraft" runat="server" CssClass="badge-draft" Text="DRAFT" Visible="false" /></h3>
                                    <p class="component-meta"><asp:Literal ID="LitLabMeta" runat="server" /></p>
                                    <div class="component-actions">
                                        <a id="LnkLab" runat="server" class="btn btn-primary btn-sm">Setup Lab Environment</a>
                                    </div>
                                </div>
                            </asp:Panel>

                            <!-- Shown when the lesson has no components yet -->
                            <asp:Panel ID="PnlEmpty" runat="server" CssClass="component-empty" Visible="false">
                                This lesson has no components yet. Use the buttons below to add a video, assessment, interactive object or virtual lab.
                            </asp:Panel>

                            <!-- Add buttons: each one only appears if the lesson does not have that component -->
                            <div id="DivAddRow" runat="server" class="component-add">
                                <span>Add:</span>
                                <button type="button" id="BtnAddVideo" runat="server" class="btn btn-outline btn-sm" onclick="openModal('modalUploadVideo')">+ Video</button>
                                <a id="LnkAddQuiz" runat="server" class="btn btn-outline btn-sm">+ Assessment</a>
                                <a id="LnkAddInteractive" runat="server" class="btn btn-outline btn-sm">+ Interactive Object</a>
                                <a id="LnkAddLab" runat="server" class="btn btn-outline btn-sm">+ Virtual Lab</a>
                            </div>

                        </div>

                        <div class="editor-footer">
                            <asp:Button ID="BtnSaveDraft" runat="server" Text="Save as Draft" CssClass="btn btn-muted" />
                            <asp:Button ID="BtnPublishModule" runat="server" OnClientClick="openModal('modalPublish'); return false;" Text="Publish Module Content" CssClass="btn btn-primary" />
                        </div>
                    </asp:Panel>
                    </section>

                </div>
            </main>
        </div>

            <!-- ============ POP-UP DIALOGS ============ -->

            <div class="modal-overlay" id="modalCourseInfo" aria-hidden="true">
                <asp:Panel ID="PnlCourseInfo" runat="server" CssClass="modal" DefaultButton="BtnSaveCourseInfo" role="dialog" aria-modal="true" aria-labelledby="title_modalCourseInfo">
                    <h3 class="modal-title" id="title_modalCourseInfo">Edit course information</h3>

                    <div class="form-group">
                        <label class="form-label" for="TxtCourseTitle">Course title</label>
                        <asp:TextBox ID="TxtCourseTitle" runat="server" CssClass="form-control" placeholder="e.g. Introduction to Python" Text="Advanced Data Structures &amp; Algorithms" />
                    </div>
                    <div class="form-group">
                        <label class="form-label" for="TxtCourseTopic">Topic</label>
                        <asp:TextBox ID="TxtCourseTopic" runat="server" CssClass="form-control" placeholder="Pick one or type a new topic" list="topicList" Text="Computer Science" />
                        <datalist id="topicList">
                            <option value="Computer Science" />
                            <option value="Mathematics" />
                            <option value="Biology" />
                            <option value="Physics" />
                        </datalist>
                    </div>
                    <div class="form-group">
                        <label class="form-label" for="TxtCourseDuration">Duration (weeks)</label>
                        <asp:TextBox ID="TxtCourseDuration" runat="server" CssClass="form-control" TextMode="Number" placeholder="e.g. 4" Text="8" />
                    </div>
                    <div class="modal-actions">
                        <button type="button" class="btn btn-cancel" onclick="closeModal('modalCourseInfo')">Cancel</button>
                        <asp:Button ID="BtnSaveCourseInfo" runat="server" Text="Save changes" CssClass="btn btn-accent" OnClick="BtnSaveCourseInfo_Click" />
                    </div>
                </asp:Panel>
            </div>

            <div class="modal-overlay" id="modalAddModule" aria-hidden="true">
                <asp:Panel ID="PnlAddModule" runat="server" CssClass="modal" DefaultButton="BtnConfirmAddModule" role="dialog" aria-modal="true" aria-labelledby="title_modalAddModule">
                    <h3 class="modal-title" id="title_modalAddModule">Add new module</h3>

                    <div class="form-group">
                        <label class="form-label" for="TxtModuleTitle">Module title</label>
                        <asp:TextBox ID="TxtModuleTitle" runat="server" CssClass="form-control" placeholder="e.g. 4. Sorting Algorithms" />
                    </div>
                    <div class="form-group">
                        <label class="form-label" for="TxtModuleDesc">Description (optional)</label>
                        <asp:TextBox ID="TxtModuleDesc" runat="server" TextMode="MultiLine" Rows="3" CssClass="form-control" />
                    </div>
                    <label class="check-row"><asp:CheckBox ID="ChkModuleDraft" runat="server" Checked="true" /> Keep as draft until published</label>
                    <div class="modal-actions">
                        <button type="button" class="btn btn-cancel" onclick="closeModal('modalAddModule')">Cancel</button>
                        <asp:Button ID="BtnConfirmAddModule" runat="server" Text="Add module" CssClass="btn btn-accent" OnClick="BtnConfirmAddModule_Click" />
                    </div>
                </asp:Panel>
            </div>

            <div class="modal-overlay" id="modalEditDetails" aria-hidden="true">
                <asp:Panel ID="PnlEditDetails" runat="server" CssClass="modal" DefaultButton="BtnSaveDetails" role="dialog" aria-modal="true" aria-labelledby="title_modalEditDetails">
                    <h3 class="modal-title" id="title_modalEditDetails">Edit lesson details</h3>
                    <div class="form-group">
                        <label class="form-label" for="TxtLessonTitle">Lesson title</label>
                        <asp:TextBox ID="TxtLessonTitle" runat="server" CssClass="form-control" placeholder="e.g. Arrays and Linked Lists" Text="Arrays and Linked Lists" />
                    </div>
                    <div class="form-group">
                        <label class="form-label" for="TxtLessonDesc">Lesson summary</label>
                        <asp:TextBox ID="TxtLessonDesc" runat="server" TextMode="MultiLine" Rows="4" CssClass="form-control" placeholder="A short overview students see before the lesson starts" />
                    </div>
                    <div class="modal-actions">
                        <button type="button" class="btn btn-cancel" onclick="closeModal('modalEditDetails')">Cancel</button>
                        <asp:Button ID="BtnSaveDetails" runat="server" OnClick="BtnSaveDetails_Click" Text="Save changes" CssClass="btn btn-accent" />
                    </div>
                </asp:Panel>
            </div>

            <div class="modal-overlay" id="modalUploadVideo" aria-hidden="true">
                <asp:Panel ID="PnlUploadVideo" runat="server" CssClass="modal" DefaultButton="BtnConfirmUpload" role="dialog" aria-modal="true" aria-labelledby="title_modalUploadVideo">
                    <h3 class="modal-title" id="title_modalUploadVideo">Upload new video</h3>
                    <div class="form-group">
                        <label class="dropzone" for="FileVideo">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M12 16V4M7 9l5-5 5 5" /><path d="M4 16v3a1 1 0 0 0 1 1h14a1 1 0 0 0 1-1v-3" /></svg>
                            <span><strong>Click to choose a video</strong> or drag it here</span>
                            <span class="form-hint">MP4, MOV or WEBM, up to 2 GB</span>
                            <span class="dropzone-file" id="videoFileName">No file selected</span>
                            <asp:FileUpload ID="FileVideo" runat="server" CssClass="dropzone-input" accept="video/*" />
                        </label>
                    </div>
                    <div class="form-group">
                        <label class="form-label" for="TxtVideoUrl">Or paste a video link</label>
                        <asp:TextBox ID="TxtVideoUrl" runat="server" CssClass="form-control" placeholder="https://..." />
                    </div>
                    <div class="modal-actions">
                        <button type="button" class="btn btn-cancel" onclick="closeModal('modalUploadVideo')">Cancel</button>
                        <asp:Button ID="BtnConfirmUpload" runat="server" OnClick="BtnConfirmUpload_Click" Text="Upload video" CssClass="btn btn-accent" />
                    </div>
                </asp:Panel>
            </div>



            <div class="modal-overlay" id="modalPublish" aria-hidden="true">
                <asp:Panel ID="PnlPublish" runat="server" CssClass="modal" DefaultButton="BtnConfirmPublish" role="dialog" aria-modal="true" aria-labelledby="title_modalPublish">
                    <h3 class="modal-title" id="title_modalPublish">Publish module content?</h3>

                    <p class="confirm-text">Publishing makes <strong><asp:Literal ID="LitPublishLesson" runat="server" /></strong> visible to all enrolled students.</p>
                    <asp:Panel ID="PnlDraftNotice" runat="server" CssClass="notice warning" Visible="false">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 3l10 18H2L12 3z" /><path d="M12 10v5" /><circle cx="12" cy="18" r=".6" fill="currentColor" /></svg>
                        <span>These components are still drafts and will stay hidden: <strong><asp:Literal ID="LitDraftList" runat="server" /></strong>.</span>
                    </asp:Panel>
                    <div class="modal-actions">
                        <button type="button" class="btn btn-cancel" onclick="closeModal('modalPublish')">Cancel</button>
                        <asp:Button ID="BtnConfirmPublish" runat="server" Text="Publish now" CssClass="btn btn-accent" />
                    </div>
                </asp:Panel>
            </div>
    </form>

    <%-- Highlight "My Courses" in the shared nav, since this page belongs to that section --%>
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

        // Esc closes any open pop-up
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                document.querySelectorAll('.modal-overlay.open').forEach(function (m) { closeModal(m.id); });
            }
        });

        // Clicking the dimmed backdrop closes the pop-up
        document.querySelectorAll('.modal-overlay').forEach(function (o) {
            o.addEventListener('mousedown', function (e) { if (e.target === o) closeModal(o.id); });
        });

        // Show the chosen video file name in the drop zone
        /** @type {HTMLInputElement} */
        var videoInput = document.getElementById('FileVideo');
        if (videoInput) {
            videoInput.addEventListener('change', function () {
                document.getElementById('videoFileName').textContent =
                    videoInput.files.length ? videoInput.files[0].name : 'No file selected';
            });
        }

        var link = document.querySelector('.nav-link[href="courses.aspx"]');
        if (link) link.classList.add('active');
    </script>
</body>
</html>
