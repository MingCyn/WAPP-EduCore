<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="courseBuilder.aspx.cs" Inherits="WAPP_EduCore.pages.Tutor.WebForm1" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>EduCore - Course Builder</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Source+Serif+4:wght@600;700&display=swap" rel="stylesheet" />
    <link href="../../Content/dashboard.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/courseBuilder.css" rel="stylesheet" type="text/css" />
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
                        <p class="page-subtitle">Advanced Data Structures &amp; Algorithms</p>
                    </div>
                    <div class="page-actions">
                        <asp:Button ID="BtnPreviewCourse" runat="server" Text="Preview Course" CssClass="btn btn-outline" />
                        <asp:Button ID="BtnPublishCourse" runat="server" Text="Publish Course" CssClass="btn btn-primary" />
                    </div>
                </div>

                <div class="builder-layout">

                    <!-- Left: Course Outline -->
                    <aside class="outline-card">
                        <h3 class="outline-title">Course Outline</h3>
                        <p class="outline-hint">Drag items to structure lessons</p>

                        <!-- Module 1 -->
                        <div class="module">
                            <div class="module-header">
                                <svg class="chevron" viewBox="0 0 12 12" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M2 4l4 4 4-4" /></svg>
                                <span>1. Foundations</span>
                            </div>
                            <div class="lesson-list">
                                <a href="?lesson=1.1" class="lesson-item">
                                    <svg class="drag-handle" viewBox="0 0 10 14" fill="currentColor"><circle cx="2" cy="2" r="1.2" /><circle cx="8" cy="2" r="1.2" /><circle cx="2" cy="7" r="1.2" /><circle cx="8" cy="7" r="1.2" /><circle cx="2" cy="12" r="1.2" /><circle cx="8" cy="12" r="1.2" /></svg>
                                    <svg class="lesson-icon" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linejoin="round"><rect x="1.5" y="3.5" width="9" height="9" rx="1.5" /><path d="M10.5 7l4-2v6l-4-2z" /></svg>
                                    <span class="lesson-name">1.1 Complexity Analysis</span>
                                    <svg class="lesson-menu" viewBox="0 0 14 14" fill="currentColor"><circle cx="2.5" cy="7" r="1.1" /><circle cx="7" cy="7" r="1.1" /><circle cx="11.5" cy="7" r="1.1" /></svg>
                                </a>
                                <a href="?lesson=1.2" class="lesson-item">
                                    <svg class="drag-handle" viewBox="0 0 10 14" fill="currentColor"><circle cx="2" cy="2" r="1.2" /><circle cx="8" cy="2" r="1.2" /><circle cx="2" cy="7" r="1.2" /><circle cx="8" cy="7" r="1.2" /><circle cx="2" cy="12" r="1.2" /><circle cx="8" cy="12" r="1.2" /></svg>
                                    <svg class="lesson-icon" viewBox="0 0 16 16" fill="currentColor"><path d="M3 1.5h6.5L13 5v9.5H3z" /><path d="M9.5 1.5V5H13" fill="none" stroke="#fff" stroke-width="1" /><path d="M5 8h6M5 10.5h6" stroke="#fff" stroke-width="1" /></svg>
                                    <span class="lesson-name">1.2 Big O Notation</span>
                                    <svg class="lesson-menu" viewBox="0 0 14 14" fill="currentColor"><circle cx="2.5" cy="7" r="1.1" /><circle cx="7" cy="7" r="1.1" /><circle cx="11.5" cy="7" r="1.1" /></svg>
                                </a>
                            </div>
                        </div>

                        <!-- Module 2 -->
                        <div class="module">
                            <div class="module-header">
                                <svg class="chevron" viewBox="0 0 12 12" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M2 4l4 4 4-4" /></svg>
                                <span>2. Core Concepts</span>
                            </div>
                            <div class="lesson-list">
                                <a href="?lesson=2.1" class="lesson-item">
                                    <svg class="drag-handle" viewBox="0 0 10 14" fill="currentColor"><circle cx="2" cy="2" r="1.2" /><circle cx="8" cy="2" r="1.2" /><circle cx="2" cy="7" r="1.2" /><circle cx="8" cy="7" r="1.2" /><circle cx="2" cy="12" r="1.2" /><circle cx="8" cy="12" r="1.2" /></svg>
                                    <svg class="lesson-icon" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linejoin="round"><rect x="1.5" y="3.5" width="9" height="9" rx="1.5" /><path d="M10.5 7l4-2v6l-4-2z" /></svg>
                                    <span class="lesson-name">2.1 Arrays &amp; Memory</span>
                                    <svg class="lesson-menu" viewBox="0 0 14 14" fill="currentColor"><circle cx="2.5" cy="7" r="1.1" /><circle cx="7" cy="7" r="1.1" /><circle cx="11.5" cy="7" r="1.1" /></svg>
                                </a>
                                <a href="?lesson=2.2" class="lesson-item">
                                    <svg class="drag-handle" viewBox="0 0 10 14" fill="currentColor"><circle cx="2" cy="2" r="1.2" /><circle cx="8" cy="2" r="1.2" /><circle cx="2" cy="7" r="1.2" /><circle cx="8" cy="7" r="1.2" /><circle cx="2" cy="12" r="1.2" /><circle cx="8" cy="12" r="1.2" /></svg>
                                    <svg class="lesson-icon" viewBox="0 0 16 16" fill="currentColor"><path d="M3 1.5h6.5L13 5v9.5H3z" /><path d="M9.5 1.5V5H13" fill="none" stroke="#fff" stroke-width="1" /><path d="M5 8h6M5 10.5h6" stroke="#fff" stroke-width="1" /></svg>
                                    <span class="lesson-name">2.2 Dynamic Arrays</span>
                                    <svg class="lesson-menu" viewBox="0 0 14 14" fill="currentColor"><circle cx="2.5" cy="7" r="1.1" /><circle cx="7" cy="7" r="1.1" /><circle cx="11.5" cy="7" r="1.1" /></svg>
                                </a>
                                <a href="?lesson=2.3" class="lesson-item active">
                                    <svg class="drag-handle" viewBox="0 0 10 14" fill="currentColor"><circle cx="2" cy="2" r="1.2" /><circle cx="8" cy="2" r="1.2" /><circle cx="2" cy="7" r="1.2" /><circle cx="8" cy="7" r="1.2" /><circle cx="2" cy="12" r="1.2" /><circle cx="8" cy="12" r="1.2" /></svg>
                                    <svg class="lesson-icon" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="2" width="12" height="12" rx="2" /><path d="M6.5 6L5 8l1.5 2M9.5 6L11 8l-1.5 2" /></svg>
                                    <span class="lesson-name">2.3 Arrays &amp; Linked Lists</span>
                                    <svg class="lesson-menu" viewBox="0 0 14 14" fill="currentColor"><circle cx="2.5" cy="7" r="1.1" /><circle cx="7" cy="7" r="1.1" /><circle cx="11.5" cy="7" r="1.1" /></svg>
                                </a>
                            </div>
                        </div>

                        <!-- Module 3 (collapsed) -->
                        <div class="module collapsed">
                            <div class="module-header">
                                <svg class="chevron" viewBox="0 0 12 12" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 2l4 4-4 4" /></svg>
                                <span>3. Non-Linear Structures</span>
                            </div>
                        </div>

                        <asp:Button ID="BtnAddModule" runat="server" Text="+  Add New Module" CssClass="btn btn-muted btn-block" />
                    </aside>

                    <!-- Right: Lesson editor -->
                    <section class="editor-card">
                        <div class="editor-header">
                            <div>
                                <h2 class="editor-title">Lesson 2.3: Arrays and Linked Lists</h2>
                                <p class="editor-meta">Topic of Core Concepts Module <span class="dot-sep">&bull;</span> Last edited 2 hours ago</p>
                            </div>
                            <asp:Button ID="BtnEditDetails" runat="server" Text="Edit Details" CssClass="btn btn-outline btn-sm" />
                        </div>

                        <div class="component-list">

                            <!-- Video component -->
                            <div class="component-card">
                                <div class="video-thumb"></div>
                                <div class="component-body">
                                    <h3 class="component-title">Video Lecture: Memory Layout and Traversals</h3>
                                    <p class="component-meta">Video Component <span class="dot-sep">&bull;</span> Duration: 24:15 mins</p>
                                    <div class="component-actions">
                                        <asp:Button ID="BtnUploadVideo" runat="server" Text="Upload New Video" CssClass="btn btn-primary btn-sm" />
                                        <asp:Button ID="BtnEditSubtitles" runat="server" Text="Edit Subtitles" CssClass="btn btn-outline btn-sm" />
                                    </div>
                                </div>
                            </div>

                            <!-- Interactive code component -->
                            <div class="component-card">
                                <div class="component-icon navy">
                                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M5 7l5 5-5 5" /><path d="M13 18h6" /></svg>
                                </div>
                                <div class="component-body">
                                    <h3 class="component-title">Interactive Code Playground: Pointer Manipulation</h3>
                                    <p class="component-meta">Interactive Object <span class="dot-sep">&bull;</span> Type: C++ Sandbox <span class="dot-sep">&bull;</span> Status: Published</p>
                                    <div class="component-actions">
                                        <asp:Button ID="BtnConfigureSandbox" runat="server" Text="Configure Sandbox" CssClass="btn btn-accent btn-sm" />
                                    </div>
                                </div>
                            </div>

                            <!-- Assessment component -->
                            <div class="component-card">
                                <div class="component-icon indigo">
                                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="9" /><path d="M9.5 9.5a2.5 2.5 0 1 1 3.5 2.3c-.7.3-1 .9-1 1.7" /><circle cx="12" cy="17" r=".6" fill="currentColor" /></svg>
                                </div>
                                <div class="component-body">
                                    <h3 class="component-title">Practice Assessment: MCQ Review</h3>
                                    <p class="component-meta">Assessment Component <span class="dot-sep">&bull;</span> 10 Questions <span class="dot-sep">&bull;</span> Auto-graded</p>
                                    <div class="component-actions">
                                        <asp:Button ID="BtnEditQuestions" runat="server" Text="Edit Questions" CssClass="btn btn-outline btn-sm" />
                                    </div>
                                </div>
                            </div>

                            <!-- Lab component (draft) -->
                            <div class="component-card">
                                <div class="component-icon amber">
                                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M9 3h6M10 3v6l-5.5 9.5A1.5 1.5 0 0 0 5.8 21h12.4a1.5 1.5 0 0 0 1.3-2.5L14 9V3" /><path d="M7.5 15h9" /></svg>
                                </div>
                                <div class="component-body">
                                    <h3 class="component-title">Lab: Singly Linked List Reversal <span class="badge-draft">DRAFT</span></h3>
                                    <p class="component-meta">Virtual Laboratory <span class="dot-sep">&bull;</span> Environment: Linux Sandbox <span class="dot-sep">&bull;</span> Needs setup</p>
                                    <div class="component-actions">
                                        <asp:Button ID="BtnSetupLab" runat="server" Text="Setup Lab Environment" CssClass="btn btn-primary btn-sm" />
                                    </div>
                                </div>
                            </div>

                        </div>

                        <div class="editor-footer">
                            <asp:Button ID="BtnSaveDraft" runat="server" Text="Save as Draft" CssClass="btn btn-muted" />
                            <asp:Button ID="BtnPublishModule" runat="server" Text="Publish Module Content" CssClass="btn btn-primary" />
                        </div>
                    </section>

                </div>
            </main>
        </div>
    </form>

    <%-- Highlight "My Courses" in the shared nav, since this page belongs to that section --%>
    <script>
        var link = document.querySelector('.nav-link[href="courses.aspx"]');
        if (link) link.classList.add('active');
    </script>
</body>
</html>
