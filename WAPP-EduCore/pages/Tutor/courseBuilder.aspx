<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="courseBuilder.aspx.cs" Inherits="WAPP_EduCore.pages.Tutor.courseBuilder" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>EduCore - Course Builder</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Source+Serif+4:wght@600;700&display=swap" rel="stylesheet" />
    <link href="../../Content/dashboard.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/courseBuilder.css?v=4" rel="stylesheet" type="text/css" />
    <style>
        .status-pill { display: inline-block; margin-left: 8px; padding: 3px 10px; border-radius: 4px; font-size: 11px; font-weight: 700; letter-spacing: 0.3px; vertical-align: middle; }
        .status-pill.draft { background-color: #fbe9d2; color: #8a5a14; }
        .status-pill.published { background-color: #e3f1e6; color: #1f7a3d; }
        .status-pill.pending { background-color: #e8ecfb; color: #2a4494; }
        .btn[disabled] { opacity: 0.6; cursor: default; }
        .live-notice { margin-bottom: 16px; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="dashboard-layout">

            <%-- Shared nav: same include as the other tutor pages --%>
            <% Server.Execute("tutorNav.aspx", false); %>

            <main class="dashboard-container">

                <!-- Page header -->
                <div class="page-header">
                    <div>
                        <h1 class="page-title">Course Builder</h1>
                        <p class="page-subtitle">
                            <asp:Label ID="LblCourseName" runat="server" />
                            <asp:Label ID="LblCourseStatus" runat="server" CssClass="status-pill draft" Text="Draft" />
                            <button type="button" class="link-edit" onclick="openModal('modalCourseInfo')">
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 20h9" /><path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4 12.5-12.5z" /></svg>
                                Edit course info
                            </button>
                        </p>
                    </div>
                    <div class="page-actions">
                        <asp:Button ID="BtnPreviewCourse" runat="server" Text="Preview Course" CssClass="btn btn-outline" OnClick="BtnPreviewCourse_Click" />
                        <%-- Draft course: Publish. Published course: Unpublish (hidden/shown from the code-behind) --%>
                        <asp:Button ID="BtnPublishCourse" runat="server" OnClientClick="openModal('modalPublish'); return false;" Text="Publish Course" CssClass="btn btn-primary" />
                        <asp:Button ID="BtnUnpublish" runat="server" OnClientClick="openModal('modalUnpublish'); return false;" Text="Unpublish Course" CssClass="btn btn-outline" Visible="false" />
                    </div>
                </div>

                <asp:Label ID="LblStatus" runat="server" CssClass="page-status" Visible="false" />

                <%-- Shown only while the course is published --%>
                <asp:Panel ID="PnlLiveNotice" runat="server" CssClass="notice warning live-notice" Visible="false">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="9" /><path d="M12 8v5" /><circle cx="12" cy="16.5" r=".6" fill="currentColor" /></svg>
                    <span>This course is <strong>published</strong>. Changes you save here are visible to students straight away.</span>
                </asp:Panel>

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

                        <asp:Button ID="BtnAddModule" runat="server" OnClientClick="openModal('modalAddModule'); return false;" Text="+ Add New Lesson" CssClass="btn btn-muted btn-block" />
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

                                <!-- Video -->
                                <asp:Panel ID="PnlVideo" runat="server" CssClass="component-card" Visible="false">
                                    <!-- Image Thumbnail Control -->
                                    <div class="video-thumb-container">
                                        <asp:Image ID="ImgVideoThumb" runat="server" CssClass="component-thumbnail" AlternateText="Video Thumbnail" />
                                    </div>

                                    <div class="component-body">
                                        <h3 class="component-title">Lesson Video</h3>
                                        <p class="component-meta">Video Component <span class="dot-sep">&bull;</span>
                                            <asp:Literal ID="LitVideoSource" runat="server" /></p>
                                        <div class="component-actions">
                                            <asp:Button ID="BtnUploadVideo" runat="server" OnClientClick="openModal('modalUploadVideo'); return false;" Text="Replace Video" CssClass="btn btn-primary btn-sm" />
                                        </div>
                                    </div>
                                </asp:Panel>

                                <!-- Paragraph Component -->
                                <asp:Panel ID="PnlParagraph" runat="server" CssClass="component-card" Visible="false">
                                    <div class="component-icon text">
                                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                            <path d="M4 6h16M4 12h16M4 18h7" />
                                        </svg>
                                    </div>
                                    <div class="component-body">
                                        <h3 class="component-title">Text Paragraph</h3>
                                        <p class="component-meta">
                                            <asp:Literal ID="LitParagraphMeta" runat="server" /></p>
                                        <div class="component-actions">
                                            <button type="button" runat="server" id="BtnEditParagraph" class="btn btn-outline btn-sm" onclick="openModal('modalEditParagraph')">Edit Text</button>
                                        </div>
                                    </div>
                                </asp:Panel>

                                <!-- Table Component -->
                                <asp:Panel ID="PnlTable" runat="server" CssClass="component-card" Visible="false">
                                    <div class="component-icon table">
                                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                            <rect x="3" y="3" width="18" height="18" rx="2" />
                                            <path d="M3 9h18M3 15h18M9 3v18" />
                                        </svg>
                                    </div>
                                    <div class="component-body">
                                        <h3 class="component-title">
                                            <asp:Literal ID="LitTableTitle" runat="server" /></h3>
                                        <p class="component-meta">Data Table Component</p>
                                        <div class="component-actions">
                                            <button type="button" runat="server" id="BtnEditTable" class="btn btn-outline btn-sm" onclick="openModal('modalEditTable')">Edit Table</button>
                                        </div>
                                    </div>
                                </asp:Panel>

                                <!-- Interactive object -->
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

                                <!-- Assessment -->
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

                                <!-- Virtual lab -->
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

                                <asp:Panel ID="PnlEmpty" runat="server" CssClass="component-empty" Visible="false">
                                    This lesson has no components yet. Use the buttons below to add a video, assessment, interactive object or virtual lab.
                                </asp:Panel>

                                <div id="DivAddRow" runat="server" class="component-add">
                                    <span>Add:</span>
                                    <button type="button" id="BtnAddVideo" runat="server" class="btn btn-outline btn-sm" onclick="openModal('modalUploadVideo')">+ Video</button>
                                    <button type="button" class="btn btn-outline btn-sm" onclick="openModal('modalEditParagraph')">+ Paragraph</button>
                                    <button type="button" class="btn btn-outline btn-sm" onclick="openModal('modalEditTable')">+ Table</button>
                                    <a id="LnkAddQuiz" runat="server" class="btn btn-outline btn-sm">+ Assessment</a>
                                    <a id="LnkAddInteractive" runat="server" class="btn btn-outline btn-sm">+ Interactive Object</a>
                                    <a id="LnkAddLab" runat="server" class="btn btn-outline btn-sm">+ Virtual Lab</a>
                                </div>

                            </div>

                            <asp:Panel ID="PnlDraftFooter" runat="server" CssClass="editor-footer">
                                <asp:Button ID="BtnSaveDraft" runat="server" OnClick="BtnSaveDraft_Click" Text="Save as Draft" CssClass="btn btn-muted" />
                                <asp:Button ID="BtnPublishModule" runat="server" OnClientClick="openModal('modalPublish'); return false;" Text="Publish Module Content" CssClass="btn btn-primary" />
                            </asp:Panel>
                        </asp:Panel>
                    </section>

                </div>
            </main>
        </div>

        <!-- ============ POP-UP DIALOGS ============ -->
        <div class="modal-overlay" id="modalCourseInfo" aria-hidden="true">
            <asp:Panel ID="PnlCourseInfo" runat="server" CssClass="modal" role="dialog" aria-modal="true" aria-labelledby="title_modalCourseInfo">
                <h3 class="modal-title" id="title_modalCourseInfo">Edit course information</h3>
                <div id="courseInfoError" class="page-status error" style="display:none"></div>

                <div class="form-group">
                    <label class="form-label" for="TxtCourseTitle">Course title</label>
                    <asp:TextBox ID="TxtCourseTitle" runat="server" CssClass="form-control" placeholder="e.g. Introduction to Python" MaxLength="150" />
                </div>
                <div class="form-group">
                    <label class="form-label" for="TxtCourseTopic">Topic</label>
                    <asp:TextBox ID="TxtCourseTopic" runat="server" CssClass="form-control" placeholder="Pick one or type a new topic" list="topicList" MaxLength="80" />
                    <datalist id="topicList">
                        <option value="Computer Science" />
                        <option value="Mathematics" />
                        <option value="Biology" />
                        <option value="Physics" />
                    </datalist>
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
                <p class="form-hint" style="margin: -8px 0 20px;">Will be saved as: <strong id="durationPreview">-</strong></p>

                <div class="form-group">
                    <label class="form-label">Course image</label>
                    <img id="ImgCourseCurrent" runat="server" visible="false" alt="Current course image"
                         style="display:block; width:100%; max-height:150px; object-fit:cover; border-radius:8px; margin-bottom:10px;" />
                    <label class="dropzone" for="FileCourseImg">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M12 16V4M7 9l5-5 5 5" /><path d="M4 16v3a1 1 0 0 0 1 1h14a1 1 0 0 0 1-1v-3" /></svg>
                        <span><strong>Click to choose an image</strong> or drag it here</span>
                        <span class="form-hint">JPG, PNG or WEBP, up to 2 MB</span>
                        <span class="dropzone-file" id="courseImgName">No new file selected</span>
                        <asp:FileUpload ID="FileCourseImg" runat="server" CssClass="dropzone-input" accept="image/png,image/jpeg,image/webp" />
                    </label>
                    <img id="imgNewPreview" alt="New course image preview"
                         style="display:none; width:100%; max-height:150px; object-fit:cover; border-radius:8px; margin-top:10px;" />
                </div>

                <div class="modal-actions">
                    <button type="button" class="btn btn-cancel" onclick="closeModal('modalCourseInfo')">Cancel</button>
                    <asp:Button ID="BtnSaveCourseInfo" runat="server" Text="Save changes" CssClass="btn btn-accent" OnClick="BtnSaveCourseInfo_Click" />
                </div>
            </asp:Panel>
        </div>

        <div class="modal-overlay" id="modalAddModule" aria-hidden="true">
            <asp:Panel ID="PnlAddModule" runat="server" CssClass="modal" role="dialog" aria-modal="true" aria-labelledby="title_modalAddModule">
                <h3 class="modal-title" id="title_modalAddModule">Add new lesson</h3>

                <div class="form-group">
                    <label class="form-label" for="TxtModuleTitle">Lesson title</label>
                    <asp:TextBox ID="TxtModuleTitle" runat="server" CssClass="form-control" placeholder="e.g. Lesson 1: Cell Structure and Function" MaxLength="200" />
                </div>
                <div class="form-group">
                    <label class="form-label" for="TxtModuleDesc">Lesson summary (optional)</label>
                    <asp:TextBox ID="TxtModuleDesc" runat="server" TextMode="MultiLine" Rows="3" CssClass="form-control" placeholder="A short overview students see before the lesson starts" />
                </div>
                <div class="modal-actions">
                    <button type="button" class="btn btn-cancel" onclick="closeModal('modalAddModule')">Cancel</button>
                    <asp:Button ID="BtnConfirmAddModule" runat="server" OnClick="BtnConfirmAddModule_Click" Text="Add lesson" CssClass="btn btn-accent" />
                </div>
            </asp:Panel>
        </div>

        <div class="modal-overlay" id="modalEditDetails" aria-hidden="true">
            <asp:Panel ID="PnlEditDetails" runat="server" CssClass="modal" role="dialog" aria-modal="true" aria-labelledby="title_modalEditDetails">
                <h3 class="modal-title" id="title_modalEditDetails">Edit lesson details</h3>
                <div class="form-group">
                    <label class="form-label" for="TxtLessonTitle">Lesson title</label>
                    <asp:TextBox ID="TxtLessonTitle" runat="server" CssClass="form-control" placeholder="e.g. Arrays and Linked Lists" />
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
            <asp:Panel ID="PnlUploadVideo" runat="server" CssClass="modal" role="dialog" aria-modal="true" aria-labelledby="title_modalUploadVideo">
                <h3 class="modal-title" id="title_modalUploadVideo">Set lesson video</h3>
                <div class="form-group">
                    <label class="form-label" for="TxtVideoUrl">Video link</label>
                    <asp:TextBox ID="TxtVideoUrl" runat="server" CssClass="form-control" placeholder="https://..." />
                    <p class="form-hint">Paste a secure (https) link to the lesson video.</p>
                </div>
                <div class="modal-actions">
                    <button type="button" class="btn btn-cancel" onclick="closeModal('modalUploadVideo')">Cancel</button>
                    <asp:Button ID="BtnConfirmUpload" runat="server" OnClick="BtnConfirmUpload_Click" Text="Save video" CssClass="btn btn-accent" />
                </div>
            </asp:Panel>
        </div>

        <div class="modal-overlay" id="modalEditParagraph" aria-hidden="true">
            <asp:Panel ID="PnlModalParagraph" runat="server" CssClass="modal" role="dialog" aria-modal="true">
                <h3 class="modal-title">Add / Edit Paragraph Text</h3>
                <div class="form-group">
                    <label class="form-label" for="TxtParagraphContent">Paragraph Content</label>
                    <asp:TextBox ID="TxtParagraphContent" runat="server" TextMode="MultiLine" Rows="5" CssClass="form-control" placeholder="Write your context or explanation here..." />
                </div>
                <div class="modal-actions">
                    <button type="button" class="btn btn-cancel" onclick="closeModal('modalEditParagraph')">Cancel</button>
                    <asp:Button ID="BtnSaveParagraph" runat="server" OnClick="BtnSaveParagraph_Click" Text="Save Paragraph" CssClass="btn btn-accent" />
                </div>
            </asp:Panel>
        </div>

        <div class="modal-overlay" id="modalEditTable" aria-hidden="true">
            <asp:Panel ID="PnlModalTable" runat="server" CssClass="modal" role="dialog" aria-modal="true">
                <h3 class="modal-title">Add / Edit Data Table</h3>
                <div class="form-group">
                    <label class="form-label" for="TxtTableTitle">Table Caption / Title</label>
                    <asp:TextBox ID="TxtTableTitle" runat="server" CssClass="form-control" placeholder="e.g. Summary of Cell Organelles" />
                </div>
                <div class="form-group">
                    <label class="form-label" for="TxtTableData">Table Rows (JSON or CSV format)</label>
                    <asp:TextBox ID="TxtTableData" runat="server" TextMode="MultiLine" Rows="4" CssClass="form-control" placeholder="Header1, Header2 | Row1Col1, Row1Col2" />
                    <p class="form-hint">Enter row data separated by commas/pipes for rendering.</p>
                </div>
                <div class="modal-actions">
                    <button type="button" class="btn btn-cancel" onclick="closeModal('modalEditTable')">Cancel</button>
                    <asp:Button ID="BtnSaveTable" runat="server" OnClick="BtnSaveTable_Click" Text="Save Table" CssClass="btn btn-accent" />
                </div>
            </asp:Panel>
        </div>

        <div class="modal-overlay" id="modalPublish" aria-hidden="true">
            <asp:Panel ID="PnlPublish" runat="server" CssClass="modal" role="dialog" aria-modal="true" aria-labelledby="title_modalPublish">
                <h3 class="modal-title" id="title_modalPublish">Publish this course?</h3>

                <p class="confirm-text">Publishing makes <strong><asp:Literal ID="LitPublishLesson" runat="server" /></strong> visible to students. Once it is published, changes you save are visible to students straight away. You can hide it again with <em>Unpublish Course</em>.</p>
                <asp:Panel ID="PnlDraftNotice" runat="server" CssClass="notice warning" Visible="false">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 3l10 18H2L12 3z" /><path d="M12 10v5" /><circle cx="12" cy="18" r=".6" fill="currentColor" /></svg>
                    <span>These components are still drafts and will stay hidden: <strong><asp:Literal ID="LitDraftList" runat="server" /></strong>.</span>
                </asp:Panel>
                <div class="modal-actions">
                    <button type="button" class="btn btn-cancel" onclick="closeModal('modalPublish')">Cancel</button>
                    <asp:Button ID="BtnConfirmPublish" runat="server" OnClick="BtnConfirmPublish_Click" Text="Publish now" CssClass="btn btn-accent" />
                </div>
            </asp:Panel>
        </div>

        <div class="modal-overlay" id="modalUnpublish" aria-hidden="true">
            <asp:Panel ID="PnlUnpublish" runat="server" CssClass="modal" role="dialog" aria-modal="true" aria-labelledby="title_modalUnpublish">
                <h3 class="modal-title" id="title_modalUnpublish">Unpublish this course?</h3>

                <p class="confirm-text">Unpublishing hides <strong><asp:Literal ID="LitUnpublishName" runat="server" /></strong> from students.</p>
                <p class="confirm-text"><asp:Literal ID="LitUnpublishInfo" runat="server" /></p>
                <div class="modal-actions">
                    <button type="button" class="btn btn-cancel" onclick="closeModal('modalUnpublish')">Keep published</button>
                    <asp:Button ID="BtnConfirmUnpublish" runat="server" OnClick="BtnConfirmUnpublish_Click" Text="Unpublish" CssClass="btn btn-accent" />
                </div>
            </asp:Panel>
        </div>
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
            if (lastFocus && typeof lastFocus.focus === 'function') lastFocus.focus();
        }

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                document.querySelectorAll('.modal-overlay.open').forEach(function (m) { closeModal(m.id); });
            }
        });

        document.querySelectorAll('.modal-overlay').forEach(function (o) {
            o.addEventListener('mousedown', function (e) { if (e.target === o) closeModal(o.id); });
        });

        document.addEventListener('keydown', function (e) {
            if (e.key !== 'Enter') return;
            var t = /** @type {HTMLInputElement | null} */ (e.target);
            if (!t || !t.closest || !t.closest('.modal')) return;
            var tag = t.tagName;
            if (tag === 'TEXTAREA' || tag === 'BUTTON') return;
            if (tag === 'INPUT' && (t.type === 'submit' || t.type === 'button')) return;
            e.preventDefault();
        });

        (function () {
            var weeks = /** @type {HTMLSelectElement | null} */ (document.getElementById('DdlCourseWeeks'));
            var hours = /** @type {HTMLSelectElement | null} */ (document.getElementById('DdlCourseHours'));
            var preview = document.getElementById('durationPreview');

            function updateDuration() {
                if (!weeks || !hours || !preview) return;
                var w = parseInt(weeks.value, 10), h = parseInt(hours.value, 10);
                preview.textContent = (w > 0 && h > 0)
                    ? w + (w === 1 ? ' week' : ' weeks') + ' (' + h + (h === 1 ? ' hr' : ' hrs') + '/week)'
                    : '-';
            }
            if (weeks) weeks.addEventListener('change', updateDuration);
            if (hours) hours.addEventListener('change', updateDuration);
            updateDuration();

            var file = /** @type {HTMLInputElement | null} */ (document.getElementById('FileCourseImg'));
            var nameEl = document.getElementById('courseImgName');
            var img = /** @type {HTMLImageElement | null} */ (document.getElementById('imgNewPreview'));
            var err = document.getElementById('courseInfoError');
            if (!file || !nameEl || !img) return;

            var fileInput = file, nameLabel = nameEl, preview2 = img;

            fileInput.addEventListener('change', function () {
                var f = fileInput.files && fileInput.files[0];
                if (err) err.style.display = 'none';
                if (!f) {
                    nameLabel.textContent = 'No new file selected';
                    preview2.style.display = 'none';
                    return;
                }
                if (f.size > 2 * 1024 * 1024) {
                    fileInput.value = '';
                    nameLabel.textContent = 'No new file selected';
                    preview2.style.display = 'none';
                    if (err) { err.textContent = 'The image must be 2 MB or smaller.'; err.style.display = 'block'; }
                    return;
                }
                nameLabel.textContent = f.name;
                var reader = new FileReader();
                reader.onload = function (e) {
                    if (e.target && typeof e.target.result === 'string') {
                        preview2.src = e.target.result;
                        preview2.style.display = 'block';
                    }
                };
                reader.readAsDataURL(f);
            });
        })();

        var link = document.querySelector('.nav-link[href="courses.aspx"]');
        if (link) link.classList.add('active');
    </script>
</body>
</html>