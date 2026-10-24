<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="courses.aspx.cs" Inherits="WAPP_EduCore.pages.Tutor.courses" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>EduCore - My Courses</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Source+Serif+4:wght@600;700&display=swap" rel="stylesheet" />
    <link href="../../Content/dashboard.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/courseBuilder.css?v=4" rel="stylesheet" type="text/css" />
    <link href="../../Content/courses.css?v=3" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
        <div class="dashboard-layout">

            <%-- Shared nav --%>
            <% Server.Execute("tutorNav.aspx", false); %>

            <main class="dashboard-container">

                <!-- Page header -->
                <div class="page-header">
                    <div>
                        <h1 class="page-title">My Courses</h1>
                        <p class="page-subtitle">Manage your created courses</p>
                    </div>
                    <div class="page-actions">
                        <asp:LinkButton ID="BtnNewCourse" runat="server" CssClass="btn btn-primary cc-create" OnClick="BtnNewCourse_Click" CausesValidation="false">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round"><path d="M12 5v14M5 12h14" /></svg>
                            Create New Course
                        </asp:LinkButton>
                    </div>
                </div>

                <asp:Label ID="LblStatus" runat="server" CssClass="page-status error" Visible="false" />

                <!-- STATE 1: Empty -->
                <asp:Panel ID="PnlEmpty" runat="server" CssClass="cc-empty-card" Visible="false">
                    <svg class="cc-empty-art" viewBox="0 0 180 180" fill="none" aria-hidden="true">
                        <circle cx="85" cy="90" r="85" fill="#fbead4" />
                        <rect x="16" y="16" width="138" height="140" rx="12" fill="#ffffff" stroke="#e1e4ea" />
                        <path d="M38 44c5-3 11-3 16 0v18c-5-3-11-3-16 0V44zM54 44c5-3 11-3 16 0v18c-5-3-11-3-16 0" stroke="#2a4494" stroke-width="2" stroke-linejoin="round" />
                        <rect x="36" y="74" width="88" height="6" rx="3" fill="#2a4494" />
                        <rect x="36" y="92" width="68" height="4" rx="2" fill="#dfe2ea" />
                        <rect x="36" y="104" width="80" height="4" rx="2" fill="#dfe2ea" />
                        <circle cx="146" cy="124" r="22" fill="#2a4494" stroke="#ffffff" stroke-width="3" />
                        <path d="M146 115v18M137 124h18" stroke="#ffffff" stroke-width="2" stroke-linecap="round" />
                    </svg>
                    <h2 class="cc-empty-title">No courses created yet</h2>
                    <p class="cc-empty-text">Your next great course starts here. Open the course builder to add your course details, organize lessons and share your expertise with students.</p>
                    <asp:LinkButton ID="BtnNewCourseEmpty" runat="server" CssClass="btn btn-primary cc-create" OnClick="BtnNewCourse_Click" CausesValidation="false">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round"><path d="M12 5v14M5 12h14" /></svg>
                        Get Started
                    </asp:LinkButton>
                    <p class="cc-empty-hint">Your courses will appear here once you create them.</p>
                </asp:Panel>

                <!-- STATE 2: List -->
                <asp:Panel ID="PnlList" runat="server" Visible="false">

                    <div class="cc-stats">
                        <div class="cc-stat"><span class="cc-stat-num"><%= TotalCount %></span><span class="cc-stat-label">Total courses</span></div>
                        <div class="cc-stat"><span class="cc-stat-num"><%= PublishedCount %></span><span class="cc-stat-label">Published</span></div>
                        <div class="cc-stat"><span class="cc-stat-num"><%= DraftCount %></span><span class="cc-stat-label">Draft</span></div>
                    </div>

                    <div class="cc-toolbar">
                        <div class="cc-chips">
                            <button type="button" class="cc-chip active" data-filter="all">All courses (<%= TotalCount %>)</button>
                            <button type="button" class="cc-chip" data-filter="published">Published (<%= PublishedCount %>)</button>
                            <button type="button" class="cc-chip" data-filter="draft">Draft (<%= DraftCount %>)</button>
                        </div>
                        <div class="cc-tools">
                            <div class="cc-search-wrap">
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><circle cx="11" cy="11" r="7" /><path d="M20 20l-3.5-3.5" /></svg>
                                <input type="text" id="ccSearch" class="cc-search" placeholder="Search your courses..." autocomplete="off" />
                            </div>
                            <select id="ccSort" class="cc-sort" aria-label="Sort courses">
                                <option value="updated">Recently updated</option>
                                <option value="created">Newest created</option>
                                <option value="title">Title (A-Z)</option>
                                <option value="students">Most students</option>
                            </select>
                        </div>
                    </div>

                    <div class="cc-grid" id="ccGrid">
                        <asp:Repeater ID="RptCourses" runat="server" OnItemCommand="RptCourses_ItemCommand">
                            <ItemTemplate>
                                <div class="cc-card"
                                     data-status='<%# Eval("StatusKey") %>'
                                     data-search='<%#: Eval("SearchText") %>'
                                     data-title='<%#: Eval("SortTitle") %>'
                                     data-students='<%# Eval("Enrolled") %>'
                                     data-updated='<%# Eval("UpdatedMs") %>'
                                     data-created='<%# Eval("CreatedMs") %>'>

                                    <%# Eval("ThumbHtml") %>

                                    <div class="cc-row">
                                        <span class='cc-badge <%# Eval("StatusClass") %>'><%#: Eval("StatusLabel") %></span>
                                        <span class="cc-modules"><%# Eval("LessonsText") %></span>
                                    </div>

                                    <asp:PlaceHolder runat="server" Visible='<%# !string.IsNullOrEmpty((string)Eval("Topic")) %>'>
                                        <div class="cc-topic"><%#: Eval("Topic") %></div>
                                    </asp:PlaceHolder>
                                    <h3 class="cc-title"><%#: Eval("Title") %></h3>

                                    <div class="cc-enrolled">
                                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="9" cy="8" r="3.2" /><path d="M3 20c0-3.3 2.7-6 6-6s6 2.7 6 6" /><circle cx="17" cy="9" r="2.4" /><path d="M17 14.2c2.4 0 4 1.8 4 4.3" /></svg>
                                        <%# Eval("EnrolledText") %>
                                    </div>
                                    <div class="cc-meta"><%#: Eval("MetaText") %></div>

                                    <div class="cc-actions">
                                        <a class="cc-view" href='courseBuilder.aspx?courseId=<%# Eval("CourseId") %>'>View Details</a>
                                        <a class="cc-edit" href='courseBuilder.aspx?courseId=<%# Eval("CourseId") %>&amp;edit=1'>
                                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4 12.5-12.5z" /></svg>
                                            Edit
                                        </a>
                                        <asp:LinkButton ID="BtnDeleteDraft" runat="server" CssClass="cc-delete-draft" 
                                            CommandName="DeleteDraft" CommandArgument='<%# Eval("CourseId") %>' 
                                            Visible='<%# Eval("StatusKey").ToString() == "draft" %>'
                                            OnClientClick="return confirm('Are you sure you want to delete this draft?');" CausesValidation="false">
                                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" width="14" height="14"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path></svg>
                                            Delete
                                        </asp:LinkButton>
                                    </div>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>

                    <div id="ccNoMatch" class="cc-nomatch" hidden>No courses match your filters.</div>

                    <p class="cc-footer">Showing <span id="ccShown"><%= TotalCount %></span> of <%= TotalCount %> courses &bull; Created by <%: TutorName %></p>
                </asp:Panel>

            </main>
        </div>
    </form>

    <script>
        // @ts-nocheck
        (function () {
            var grid = document.getElementById('ccGrid');
            if (!grid) return;

            var cards = Array.prototype.slice.call(grid.querySelectorAll('.cc-card'));
            var chips = document.querySelectorAll('.cc-chip');
            var search = document.getElementById('ccSearch');
            var sort = document.getElementById('ccSort');
            var shownEl = document.getElementById('ccShown');
            var noMatch = document.getElementById('ccNoMatch');
            var filter = 'all';

            function apply() {
                var q = search.value.trim().toLowerCase();
                var shown = 0;
                cards.forEach(function (c) {
                    var okStatus = filter === 'all' || c.getAttribute('data-status') === filter;
                    var okSearch = c.getAttribute('data-search').indexOf(q) !== -1;
                    c.hidden = !(okStatus && okSearch);
                    if (!c.hidden) shown++;
                });
                shownEl.textContent = shown;
                noMatch.hidden = shown > 0;
            }

            function num(card, attr) { return parseFloat(card.getAttribute(attr)) || 0; }

            function applySort() {
                var mode = sort.value;
                cards.sort(function (a, b) {
                    if (mode === 'title') return a.getAttribute('data-title').localeCompare(b.getAttribute('data-title'));
                    if (mode === 'students') return num(b, 'data-students') - num(a, 'data-students');
                    if (mode === 'created') return num(b, 'data-created') - num(a, 'data-created');
                    return num(b, 'data-updated') - num(a, 'data-updated');
                });
                cards.forEach(function (c) { grid.appendChild(c); });
            }

            chips.forEach(function (chip) {
                chip.addEventListener('click', function () {
                    chips.forEach(function (c) { c.classList.remove('active'); });
                    chip.classList.add('active');
                    filter = chip.getAttribute('data-filter');
                    apply();
                });
            });
            search.addEventListener('input', apply);
            sort.addEventListener('change', applySort);
        })();

        var navLink = document.querySelector('.nav-link[href="courses.aspx"]');
        if (navLink) navLink.classList.add('active');
    </script>
</body>
</html>