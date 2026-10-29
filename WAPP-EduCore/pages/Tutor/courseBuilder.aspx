<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="courseBuilder.aspx.cs" Inherits="WAPP_EduCore.pages.Tutor.courseBuilder" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>EduCore - Course Builder</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Source+Serif+4:wght@600;700&display=swap" rel="stylesheet" />
    <link href="../../Content/dashboard.css?v=3" rel="stylesheet" type="text/css" />
    <link href="../../Content/courseBuilder.css?v=4" rel="stylesheet" type="text/css" />
    <style>
        .status-pill { display: inline-block; margin-left: 8px; padding: 3px 10px; border-radius: 4px; font-size: 11px; font-weight: 700; letter-spacing: 0.3px; vertical-align: middle; }
        .status-pill.draft { background-color: #fbe9d2; color: #8a5a14; }
        .status-pill.published { background-color: #e3f1e6; color: #1f7a3d; }
        .status-pill.pending { background-color: #e8ecfb; color: #2a4494; }
        .btn[disabled] { opacity: 0.6; cursor: default; }
        .live-notice { margin-bottom: 16px; }

        /* Breadcrumb navigation */
        .breadcrumb-nav { font-size: 13px; color: #64748b; margin-bottom: 8px; display: flex; align-items: center; gap: 6px; }
        .breadcrumb-nav a { color: #4f46e5; text-decoration: none; }
        .breadcrumb-nav a:hover { text-decoration: underline; }
        .breadcrumb-separator { color: #cbd5e1; }

        /* Lesson content: heading / paragraph / list / image / video, in the order students see them */
        .block-section { margin-bottom: 22px; }
        .block-heading { font-size: 15px; font-weight: 600; margin: 0 0 4px; }
        .block-hint { font-size: 12px; color: #6b7285; margin: 0 0 12px; }
        .block-item { display: flex; align-items: center; gap: 12px; padding: 10px 14px; margin-bottom: 8px; background: #ffffff; border: 1px solid #e4e2dd; border-radius: 8px; }
        .block-type { flex: none; min-width: 88px; padding: 3px 8px; font-size: 11px; font-weight: 700; text-align: center; text-transform: uppercase; letter-spacing: 0.3px; color: #2a4494; background: #e8ecfb; border-radius: 4px; }
        .block-thumb { flex: none; width: 72px; height: 48px; object-fit: cover; border-radius: 6px; background: #f1f2f6; }
        .block-preview { flex: 1; min-width: 0; font-size: 13px; line-height: 1.5; color: #3d4260; overflow-wrap: anywhere; }
        .block-actions { flex: none; display: flex; gap: 6px; }
        .block-actions .btn { padding: 4px 10px; }
        .aspNetDisabled { opacity: 0.35; pointer-events: none; }
        #modalText textarea.form-control { line-height: 1.5; }
        .input-row { display: flex; gap: 8px; margin-bottom: 8px; }
        .input-row .btn-cancel { padding: 0 12px; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="dashboard-layout">

            <%-- Shared nav: same include as the other tutor pages --%>
            <% Server.Execute("tutorNav.aspx", false); %>

            <main class="dashboard-container">

                <!-- Breadcrumb -->
                <div class="breadcrumb-nav">
                    <a href="courses.aspx">My Courses</a>
                    <span class="breadcrumb-separator">&gt;</span>
                    <a href='courseDetails.aspx?courseId=<%= CourseIdValue %>'>
                        <asp:Label ID="LitCrumbTitle" runat="server" Text="Course Details" />
                    </a>
                    <span class="breadcrumb-separator">&gt;</span>
                    <span style="color: #1e293b; font-weight: 500;">Course Builder</span>
                </div>

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
                        <a class="btn btn-outline" href='courseDetails.aspx?courseId=<%= CourseIdValue %>'>Preview Course</a>
                        <%-- Draft course: Publish. Published course: Unpublish (toggled from the code-behind) --%>
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
                        <p class="outline-hint">Select a lesson to edit it</p>

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
                                <div style="display: flex; gap: 8px; flex: none;">
                                    <asp:Button ID="BtnEditDetails" runat="server" OnClientClick="openModal('modalEditDetails'); return false;" Text="Edit Details" CssClass="btn btn-outline btn-sm" />
                                    <button type="button" class="btn btn-outline btn-sm" onclick="openModal('modalDeleteLesson')">Delete Lesson</button>
                                </div>
                            </div>

                            <asp:Panel ID="PnlSummary" runat="server" Visible="false">
                                <p class="editor-summary"><asp:Literal ID="LitSummary" runat="server" /></p>
                            </asp:Panel>

                            <div class="component-list">

                                <%-- Lesson content, saved in lesson.lesson_content in exactly this order --%>
                                <div class="block-section">
                                    <h3 class="block-heading">Lesson content</h3>
                                    <p class="block-hint">Students see the lesson in the order listed here. Use the arrows to move a heading, paragraph, list, image or video up or down.</p>

                                    <asp:Repeater ID="RptBlocks" runat="server" OnItemCommand="RptBlocks_ItemCommand">
                                        <ItemTemplate>
                                            <div class="block-item">
                                                <span class="block-type"><%#: Eval("TypeLabel") %></span>
                                                <asp:PlaceHolder runat="server" Visible='<%# !string.IsNullOrEmpty((string)Eval("ThumbUrl")) %>'>
                                                    <img class="block-thumb" src='<%#: Eval("ThumbUrl") %>' alt="" />
                                                </asp:PlaceHolder>
                                                <div class="block-preview"><%#: Eval("Preview") %></div>
                                                <div class="block-actions">
                                                    <asp:LinkButton runat="server" CommandName="up" CommandArgument='<%# Eval("Index") %>' CssClass="btn btn-outline btn-sm" Enabled='<%# (bool)Eval("CanMoveUp") %>' CausesValidation="false" ToolTip="Move up">&uarr;</asp:LinkButton>
                                                    <asp:LinkButton runat="server" CommandName="down" CommandArgument='<%# Eval("Index") %>' CssClass="btn btn-outline btn-sm" Enabled='<%# (bool)Eval("CanMoveDown") %>' CausesValidation="false" ToolTip="Move down">&darr;</asp:LinkButton>
                                                    <asp:LinkButton runat="server" CommandName="edit" CommandArgument='<%# Eval("Index") %>' CssClass="btn btn-outline btn-sm" Visible='<%# (bool)Eval("CanEdit") %>' CausesValidation="false">Edit</asp:LinkButton>
                                                    <button type="button" class="btn btn-outline btn-sm" data-index='<%# Eval("Index") %>' data-type='<%#: Eval("TypeKey") %>' data-text='<%#: Eval("Preview") %>' onclick="askRemoveBlock(this)">Delete</button>
                                                </div>
                                            </div>
                                        </ItemTemplate>
                                    </asp:Repeater>

                                    <asp:Panel ID="PnlNoBlocks" runat="server" CssClass="component-empty" Visible="false">
                                        No content yet. Use the buttons below to add a heading and paragraph, a list, an image or a video.
                                    </asp:Panel>

                                    <div class="component-add">
                                        <span>Add:</span>
                                        <button type="button" class="btn btn-outline btn-sm" onclick="openTextModal()">+ Heading &amp; Paragraph</button>
                                        <button type="button" class="btn btn-outline btn-sm" onclick="openListModal()">+ List</button>
                                        <button type="button" id="BtnAddImage" runat="server" class="btn btn-outline btn-sm" onclick="openImageModal()">+ Image</button>
                                        <button type="button" id="BtnAddVideo" runat="server" class="btn btn-outline btn-sm" onclick="openModal('modalUploadVideo')">+ Video</button>
                                        <a id="LnkAddQuiz" runat="server" class="btn btn-outline btn-sm">+ Assessment</a>
                                    </div>
                                </div>

                                <!-- Assessment -->
                                <asp:Panel ID="PnlAssessment" runat="server" CssClass="component-card" Visible="false">
                                    <div class="component-icon indigo">
                                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="9" /><path d="M9.5 9.5a2.5 2.5 0 1 1 3.5 2.3c-.7.3-1 .9-1 1.7" /><circle cx="12" cy="17" r=".6" fill="currentColor" /></svg>
                                    </div>
                                    <div class="component-body">
                                        <h3 class="component-title">Lesson Assessment</h3>
                                        <p class="component-meta"><asp:Literal ID="LitAssessmentMeta" runat="server" /></p>
                                        <div class="component-actions">
                                            <div class="component-actions">
                                                <a id="LnkAssessment" runat="server" class="btn btn-outline btn-sm">Edit Questions</a>
                                                <button type="button" class="btn btn-outline btn-sm" onclick="openModal('modalDeleteAssessment')">Delete Assessment</button>
                                            </div>
                                        </div>
                                    </div>
                                </asp:Panel>

                            </div>

                            <asp:Panel ID="PnlDraftFooter" runat="server" CssClass="editor-footer">
                                <asp:Button ID="BtnSaveDraft" runat="server" OnClick="BtnSaveDraft_Click" Text="Save as Draft" CssClass="btn btn-muted" />
                                <asp:Button ID="BtnPublishModule" runat="server" OnClientClick="openModal('modalPublish'); return false;" Text="Publish Module Content" CssClass="btn btn-primary" />
                            </asp:Panel>
                        </asp:Panel>

                        <%-- Delete assessment --%>
                        <div class="modal-overlay" id="modalDeleteAssessment" aria-hidden="true">
                            <asp:Panel ID="PnlDeleteAssessment" runat="server" CssClass="modal" role="dialog" aria-modal="true" aria-labelledby="title_modalDeleteAssessment">
                                <h3 class="modal-title" id="title_modalDeleteAssessment">Delete this assessment?</h3>
                                <p class="confirm-text">This will permanently delete the assessment and all of its questions from this lesson. This cannot be undone.</p>
                                <div class="modal-actions">
                                    <button type="button" class="btn btn-cancel" onclick="closeModal('modalDeleteAssessment')">Cancel</button>
                                    <asp:Button ID="BtnConfirmDeleteAssessment" runat="server" OnClick="BtnConfirmDeleteAssessment_Click" Text="Delete assessment" CssClass="btn btn-accent" />
                                </div>
                            </asp:Panel>
                        </div>
                    </section>

                </div>
            </main>
        </div>

        <!-- ============ POP-UP DIALOGS (each one is a top-level overlay: never nest them) ============ -->

        <%-- Course info --%>
        <div class="modal-overlay" id="modalCourseInfo" aria-hidden="true">
            <asp:Panel ID="PnlCourseInfo" runat="server" CssClass="modal" role="dialog" aria-modal="true" aria-labelledby="title_modalCourseInfo">
                <h3 class="modal-title" id="title_modalCourseInfo">Edit course information</h3>
                <div id="courseInfoError" class="page-status error" style="display: none"></div>

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
                    <label class="form-label">Learning Outcomes</label>
                    <div id="outcomesContainer"></div>
                    <button type="button" class="btn btn-outline btn-sm" onclick="addOutcomeInput()" style="margin-top: 8px;">+ Add Outcome</button>
                    <span class="form-hint" style="margin-top: 6px;">Each box represents a new line in the database so the student view can read them point by point.</span>
                </div>

                <div class="form-group">
                    <label class="form-label">References</label>
                    <div id="referencesContainer"></div>
                    <button type="button" class="btn btn-outline btn-sm" onclick="addReferenceInput()" style="margin-top: 8px;">+ Add Reference</button>
                    <span class="form-hint" style="margin-top: 6px;">Add source references or citations for this course.</span>
                </div>

                <div class="form-group">
                    <label class="form-label">Course image</label>
                    <img id="ImgCourseCurrent" runat="server" visible="false" alt="Current course image"
                        style="display: block; width: 100%; max-height: 150px; object-fit: cover; border-radius: 8px; margin-bottom: 10px;" />
                    <label class="dropzone" for="FileCourseImg">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M12 16V4M7 9l5-5 5 5" /><path d="M4 16v3a1 1 0 0 0 1 1h14a1 1 0 0 0 1-1v-3" /></svg>
                        <span><strong>Click to choose an image</strong> or drag it here</span>
                        <span class="form-hint">JPG, PNG or WEBP, up to 2 MB</span>
                        <span class="dropzone-file" id="courseImgName">No new file selected</span>
                        <asp:FileUpload ID="FileCourseImg" runat="server" CssClass="dropzone-input" accept="image/png,image/jpeg,image/webp" />
                    </label>
                    <img id="imgNewPreview" alt="New course image preview"
                        style="display: none; width: 100%; max-height: 150px; object-fit: cover; border-radius: 8px; margin-top: 10px;" />
                </div>

                <div class="modal-actions">
                    <button type="button" class="btn btn-cancel" onclick="closeModal('modalCourseInfo')">Cancel</button>
                    <asp:Button ID="BtnSaveCourseInfo" runat="server" Text="Save changes" CssClass="btn btn-accent" OnClick="BtnSaveCourseInfo_Click" />
                </div>
            </asp:Panel>
        </div>

        <%-- Add lesson --%>
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

        <%-- Edit lesson details --%>
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

        <%-- Add / edit text: a heading and a paragraph together (or just one of them) --%>
        <div class="modal-overlay" id="modalText" aria-hidden="true">
            <asp:Panel ID="PnlText" runat="server" CssClass="modal" role="dialog" aria-modal="true" aria-labelledby="title_modalText">
                <h3 class="modal-title" id="title_modalText">Add heading &amp; paragraph</h3>
                <div id="textError" class="page-status error" style="display: none"></div>

                <%-- HdnTextIndex: -1 = new content, otherwise the position of the block being edited.
                     HdnTextMode:  both | heading | paragraph --%>
                <asp:HiddenField ID="HdnTextIndex" runat="server" Value="-1" />
                <asp:HiddenField ID="HdnTextMode" runat="server" Value="both" />

                <div class="form-group" id="groupHeading">
                    <label class="form-label" for="TxtHeading">Heading <span id="headingOptional">(optional)</span></label>
                    <asp:TextBox ID="TxtHeading" runat="server" CssClass="form-control" MaxLength="200" placeholder="e.g. Main Types of Cells" />
                </div>
                <div class="form-group" id="groupParagraph">
                    <label class="form-label" for="TxtParagraph">Paragraph <span id="paragraphOptional">(optional)</span></label>
                    <asp:TextBox ID="TxtParagraph" runat="server" TextMode="MultiLine" Rows="7" CssClass="form-control" placeholder="Write the explanation students will read under the heading..." />
                    <p class="form-hint" id="textHint">Add a heading, a paragraph, or both. They are saved as separate blocks, heading first, so you can move them independently.</p>
                </div>

                <div class="modal-actions">
                    <button type="button" class="btn btn-cancel" onclick="closeModal('modalText')">Cancel</button>
                    <asp:Button ID="BtnSaveText" runat="server" OnClick="BtnSaveText_Click" Text="Save" CssClass="btn btn-accent" />
                </div>
            </asp:Panel>
        </div>

        <%-- Add / edit a list: one box per item (its own overlay, NOT inside modalText) --%>
        <div class="modal-overlay" id="modalList" aria-hidden="true">
            <asp:Panel ID="PnlList" runat="server" CssClass="modal" role="dialog" aria-modal="true" aria-labelledby="title_modalList">
                <h3 class="modal-title" id="title_modalList">Add list</h3>
                <div id="listError" class="page-status error" style="display: none"></div>

                <%-- -1 = adding a new list, otherwise the position of the list being edited --%>
                <asp:HiddenField ID="HdnListIndex" runat="server" Value="-1" />

                <div class="form-group">
                    <label class="form-label">List items</label>
                    <div id="listItemsContainer"></div>
                    <button type="button" class="btn btn-outline btn-sm" onclick="addListItemInput()" style="margin-top: 8px;">+ Add item</button>
                    <span class="form-hint" style="margin-top: 6px;">One box per item. Empty boxes are ignored.</span>
                </div>

                <div class="modal-actions">
                    <button type="button" class="btn btn-cancel" onclick="closeModal('modalList')">Cancel</button>
                    <asp:Button ID="BtnSaveList" runat="server" OnClick="BtnSaveList_Click" Text="Save list" CssClass="btn btn-accent" />
                </div>
            </asp:Panel>
        </div>

        <%-- Add / edit an image (one image per lesson; to replace it, remove it first) --%>
        <div class="modal-overlay" id="modalImage" aria-hidden="true">
            <asp:Panel ID="PnlImage" runat="server" CssClass="modal" role="dialog" aria-modal="true" aria-labelledby="title_modalImage">
                <h3 class="modal-title" id="title_modalImage">Add image</h3>
                <div id="imageError" class="page-status error" style="display: none"></div>

                <%-- -1 = adding a new image, otherwise the position of the image being edited --%>
                <asp:HiddenField ID="HdnImageIndex" runat="server" Value="-1" />

                <div class="form-group">
                    <label class="form-label">Image</label>
                    <img id="ImgBlockCurrent" runat="server" visible="false" alt="Current image"
                        style="display: block; width: 100%; max-height: 200px; object-fit: contain; border-radius: 8px; margin-bottom: 10px; background: #f1f2f6;" />
                    <button type="button" id="btnRemoveImage" class="btn btn-outline btn-sm"
                        style="display: none; margin-bottom: 10px;" onclick="askRemoveCurrentImage()">Remove current image</button>

                    <%-- Only shown when adding --%>
                    <div id="imageUploadArea">
                        <label class="dropzone" for="FileBlockImage">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M12 16V4M7 9l5-5 5 5" /><path d="M4 16v3a1 1 0 0 0 1 1h14a1 1 0 0 0 1-1v-3" /></svg>
                            <span><strong>Click to choose an image</strong> or drag it here</span>
                            <span class="form-hint">JPG, PNG or WEBP, up to 2 MB</span>
                            <span class="dropzone-file" id="blockImgName">No file selected</span>
                            <asp:FileUpload ID="FileBlockImage" runat="server" CssClass="dropzone-input" accept="image/png,image/jpeg,image/webp" />
                        </label>
                        <img id="imgBlockPreview" alt="Preview of the chosen image"
                            style="display: none; width: 100%; max-height: 220px; object-fit: contain; border-radius: 8px; margin-top: 10px; background: #f1f2f6;" />
                    </div>
                </div>
                <div class="form-group">
                    <label class="form-label" for="TxtImageCaption">Caption (optional)</label>
                    <asp:TextBox ID="TxtImageCaption" runat="server" CssClass="form-control" MaxLength="200" placeholder="e.g. An image of eukaryotic and prokaryotic cell structures" />
                </div>

                <div class="modal-actions">
                    <button type="button" class="btn btn-cancel" onclick="closeModal('modalImage')">Cancel</button>
                    <asp:Button ID="BtnSaveImage" runat="server" OnClick="BtnSaveImage_Click" Text="Save image" CssClass="btn btn-accent" />
                </div>
            </asp:Panel>
        </div>

        <%-- Video --%>
        <div class="modal-overlay" id="modalUploadVideo" aria-hidden="true">
            <asp:Panel ID="PnlUploadVideo" runat="server" CssClass="modal" role="dialog" aria-modal="true" aria-labelledby="title_modalUploadVideo">
                <h3 class="modal-title" id="title_modalUploadVideo">Set lesson video</h3>
                <div class="form-group">
                    <label class="form-label" for="TxtVideoUrl">Video link</label>
                    <asp:TextBox ID="TxtVideoUrl" runat="server" CssClass="form-control" placeholder="https://..." />
                    <p class="form-hint">Paste a secure (https) link to the lesson video.</p>
                </div>
                <div class="form-group">
                    <label class="form-label" for="TxtVideoCaption">Caption (optional)</label>
                    <asp:TextBox ID="TxtVideoCaption" runat="server" CssClass="form-control" MaxLength="200" placeholder="e.g. A video introduction to cell theory" />
                </div>
                <div class="modal-actions">
                    <button type="button" class="btn btn-cancel" onclick="closeModal('modalUploadVideo')">Cancel</button>
                    <asp:Button ID="BtnConfirmUpload" runat="server" OnClick="BtnConfirmUpload_Click" Text="Save video" CssClass="btn btn-accent" />
                </div>
            </asp:Panel>
        </div>

        <%-- Confirm deleting a heading / paragraph / list / image / video --%>
        <div class="modal-overlay" id="modalRemoveBlock" aria-hidden="true">
            <div class="modal" role="dialog" aria-modal="true" aria-labelledby="title_modalRemoveBlock">
                <h3 class="modal-title" id="title_modalRemoveBlock">Delete this content?</h3>
                <p class="confirm-text">This will delete <strong id="removeBlockText"></strong> from the lesson. This cannot be undone.</p>
                <asp:HiddenField ID="HdnRemoveBlockIndex" runat="server" />
                <asp:HiddenField ID="HdnRemoveBlockType" runat="server" />
                <div class="modal-actions">
                    <button type="button" class="btn btn-cancel" onclick="closeModal('modalRemoveBlock')">Cancel</button>
                    <asp:Button ID="BtnConfirmRemoveBlock" runat="server" OnClick="BtnConfirmRemoveBlock_Click" Text="Delete" CssClass="btn btn-accent" />
                </div>
            </div>
        </div>

        <%-- General message / error pop-up --%>
        <div class="modal-overlay" id="modalMessage" aria-hidden="true">
            <div class="modal" role="alertdialog" aria-modal="true" aria-labelledby="title_modalMessage">
                <h3 class="modal-title" id="title_modalMessage">Something went wrong</h3>
                <p class="confirm-text" id="messageText"></p>
                <div class="modal-actions">
                    <button type="button" class="btn btn-accent" onclick="closeModal('modalMessage')">OK</button>
                </div>
            </div>
        </div>

        <%-- Delete lesson --%>
        <div class="modal-overlay" id="modalDeleteLesson" aria-hidden="true">
            <asp:Panel ID="PnlDeleteLesson" runat="server" CssClass="modal" role="dialog" aria-modal="true" aria-labelledby="title_modalDeleteLesson">
                <h3 class="modal-title" id="title_modalDeleteLesson">Delete this lesson?</h3>
                <p class="confirm-text">This will permanently delete <strong><asp:Literal ID="LitDeleteLessonName" runat="server" /></strong> and all of its content, including uploaded images. This cannot be undone.</p>
                <div class="modal-actions">
                    <button type="button" class="btn btn-cancel" onclick="closeModal('modalDeleteLesson')">Cancel</button>
                    <asp:Button ID="BtnConfirmDeleteLesson" runat="server" OnClick="BtnConfirmDeleteLesson_Click" Text="Delete lesson" CssClass="btn btn-accent" />
                </div>
            </asp:Panel>
        </div>

        <%-- Publish --%>
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

        <%-- Unpublish --%>
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
        var MAX_IMAGE_BYTES = 2 * 1024 * 1024; // 2 MB

        // ---------- Modal basics ----------
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

        // Enter inside a pop-up must never submit the form: only the pop-up's own button saves.
        document.addEventListener('keydown', function (e) {
            if (e.key !== 'Enter') return;
            var t = e.target;
            if (!t || !t.closest || !t.closest('.modal')) return;
            var tag = t.tagName;
            if (tag === 'TEXTAREA' || tag === 'BUTTON') return;
            if (tag === 'INPUT' && (t.type === 'submit' || t.type === 'button')) return;
            e.preventDefault();
        });

        function showPopup(title, message) {
            document.getElementById('title_modalMessage').textContent = title;
            document.getElementById('messageText').textContent = message;
            openModal('modalMessage');
        }

        var navLink = document.querySelector('.nav-link[href="courses.aspx"]');
        if (navLink) navLink.classList.add('active');

        // ---------- Shared helper: a text box with a remove button ----------
        function addInputRow(containerId, inputName, placeholder, value, maxLength) {
            var container = document.getElementById(containerId);
            var row = document.createElement('div');
            row.className = 'input-row';

            var input = document.createElement('input');
            input.type = 'text';
            input.name = inputName;
            input.className = 'form-control';
            input.placeholder = placeholder;
            input.value = value || '';
            if (maxLength) input.maxLength = maxLength;

            var removeBtn = document.createElement('button');
            removeBtn.type = 'button';
            removeBtn.className = 'btn btn-cancel';
            removeBtn.textContent = '\u2715';
            removeBtn.onclick = function () {
                if (container.children.length > 1) container.removeChild(row);
                else input.value = '';
            };

            row.appendChild(input);
            row.appendChild(removeBtn);
            container.appendChild(row);
            return input;
        }

        // ---------- Course info: duration preview + new course image preview ----------
        (function () {
            var weeks = document.getElementById('DdlCourseWeeks');
            var hours = document.getElementById('DdlCourseHours');
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

            var file = document.getElementById('FileCourseImg');
            var nameEl = document.getElementById('courseImgName');
            var img = document.getElementById('imgNewPreview');
            var err = document.getElementById('courseInfoError');
            if (!file || !nameEl || !img) return;

            file.addEventListener('change', function () {
                var f = file.files && file.files[0];
                if (err) err.style.display = 'none';
                if (!f) {
                    nameEl.textContent = 'No new file selected';
                    img.style.display = 'none';
                    return;
                }
                if (f.size > MAX_IMAGE_BYTES) {
                    file.value = '';
                    nameEl.textContent = 'No new file selected';
                    img.style.display = 'none';
                    if (err) { err.textContent = 'The image must be 2 MB or smaller.'; err.style.display = 'block'; }
                    return;
                }
                nameEl.textContent = f.name;
                var reader = new FileReader();
                reader.onload = function (e) {
                    if (e.target && typeof e.target.result === 'string') {
                        img.src = e.target.result;
                        img.style.display = 'block';
                    }
                };
                reader.readAsDataURL(f);
            });
        })();

        // ---------- Learning outcomes & references ----------
        function addOutcomeInput(value) {
            addInputRow('outcomesContainer', 'learningOutcome', 'e.g. Identify variables and write a clear hypothesis', value);
        }

        function addReferenceInput(value) {
            addInputRow('referencesContainer', 'courseReference', 'e.g. Gan, W. Y. et al. (2019). Biology Form 4 KSSM.', value);
        }

        document.addEventListener('DOMContentLoaded', function () {
            var outcomes = document.getElementById('outcomesContainer');
            if (outcomes && outcomes.children.length === 0) addOutcomeInput();
            var refs = document.getElementById('referencesContainer');
            if (refs && refs.children.length === 0) addReferenceInput();
        });

        // ---------- Heading & paragraph pop-up ----------
        var TEXT_MODES = {
            both:      { title: 'Add heading & paragraph', heading: true,  paragraph: true,  headingOptional: true,  paragraphOptional: true  },
            heading:   { title: 'Edit heading',            heading: true,  paragraph: false, headingOptional: false, paragraphOptional: true  },
            paragraph: { title: 'Edit paragraph',          heading: false, paragraph: true,  headingOptional: true,  paragraphOptional: false }
        };

        function setTextMode(mode) {
            var cfg = TEXT_MODES[mode] || TEXT_MODES.both;
            document.getElementById('HdnTextMode').value = TEXT_MODES[mode] ? mode : 'both';
            document.getElementById('title_modalText').textContent = cfg.title;
            document.getElementById('groupHeading').style.display = cfg.heading ? '' : 'none';
            document.getElementById('groupParagraph').style.display = cfg.paragraph ? '' : 'none';
            document.getElementById('headingOptional').style.display = cfg.headingOptional ? '' : 'none';
            document.getElementById('paragraphOptional').style.display = cfg.paragraphOptional ? '' : 'none';
            document.getElementById('textHint').style.display = (mode === 'both' || !TEXT_MODES[mode]) ? '' : 'none';
        }

        function openTextModal() {
            document.getElementById('HdnTextIndex').value = '-1';
            document.getElementById('TxtHeading').value = '';
            document.getElementById('TxtParagraph').value = '';
            document.getElementById('textError').style.display = 'none';
            setTextMode('both');
            openModal('modalText');
        }

        // ---------- List pop-up: one input box per item ----------
        function addListItemInput(value) {
            return addInputRow('listItemsContainer', 'listItem', 'e.g. Cells are the basic unit of life', value, 500);
        }

        // index = -1 for a new list, otherwise the position of the list being edited
        function fillListModal(items, index) {
            document.getElementById('HdnListIndex').value = String(index);
            document.getElementById('title_modalList').textContent = index < 0 ? 'Add list' : 'Edit list';
            document.getElementById('listError').style.display = 'none';

            document.getElementById('listItemsContainer').innerHTML = '';
            if (items && items.length) items.forEach(function (v) { addListItemInput(v); });
            else addListItemInput();
        }

        function openListModal() {
            fillListModal([], -1);
            openModal('modalList');
        }

        // ---------- Image pop-up with live preview ----------
        function openImageModal() {
            document.getElementById('HdnImageIndex').value = '-1';
            document.getElementById('TxtImageCaption').value = '';
            var input = document.getElementById('FileBlockImage');
            if (input) input.value = '';
            document.getElementById('blockImgName').textContent = 'No file selected';
            document.getElementById('imgBlockPreview').style.display = 'none';
            document.getElementById('imageError').style.display = 'none';
            var current = document.getElementById('ImgBlockCurrent');
            if (current) current.style.display = 'none';
            var rb = document.getElementById('btnRemoveImage');
            if (rb) rb.style.display = 'none';
            document.getElementById('imageUploadArea').style.display = '';
            document.getElementById('title_modalImage').textContent = 'Add image';
            openModal('modalImage');
        }

        (function () {
            var input = document.getElementById('FileBlockImage');
            var nameEl = document.getElementById('blockImgName');
            var preview = document.getElementById('imgBlockPreview');
            var err = document.getElementById('imageError');
            if (!input || !nameEl || !preview) return;

            function reset(message) {
                input.value = '';
                nameEl.textContent = 'No file selected';
                preview.style.display = 'none';
                if (message && err) { err.textContent = message; err.style.display = 'block'; }
            }

            input.addEventListener('change', function () {
                var f = input.files && input.files[0];
                if (err) err.style.display = 'none';
                if (!f) { reset(); return; }
                if (!/^image\/(jpeg|png|webp)$/.test(f.type)) { reset('Please choose a JPG, PNG or WEBP image.'); return; }
                if (f.size > MAX_IMAGE_BYTES) { reset('The image must be 2 MB or smaller.'); return; }

                nameEl.textContent = f.name;
                var reader = new FileReader();
                reader.onload = function (e) {
                    if (e.target && typeof e.target.result === 'string') {
                        preview.src = e.target.result;
                        preview.style.display = 'block';
                    }
                };
                reader.readAsDataURL(f);
            });
        })();

        // ---------- Delete a content block ----------
        function askRemoveBlock(btn) {
            var text = btn.getAttribute('data-text') || '';
            document.getElementById('HdnRemoveBlockIndex').value = btn.getAttribute('data-index');
            document.getElementById('HdnRemoveBlockType').value = btn.getAttribute('data-type');
            document.getElementById('removeBlockText').textContent = text ? '\u201C' + text + '\u201D' : 'this item';
            openModal('modalRemoveBlock');
        }

        // "Remove current image" inside the edit-image dialog reuses the delete confirmation
        function askRemoveCurrentImage() {
            var idx = document.getElementById('HdnImageIndex').value;
            if (!idx || parseInt(idx, 10) < 0) return;
            closeModal('modalImage');
            document.getElementById('HdnRemoveBlockIndex').value = idx;
            document.getElementById('HdnRemoveBlockType').value = 'image';
            document.getElementById('removeBlockText').textContent = 'this image';
            openModal('modalRemoveBlock');
        }
    </script>
</body>
</html>
