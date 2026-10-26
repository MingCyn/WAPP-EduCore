<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="messages.aspx.cs" Inherits="WAPP_EduCore.pages.Tutor.messages" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>EduCore - Communications Desk</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Source+Serif+4:wght@600;700&display=swap" rel="stylesheet" />
    <link href="../../Content/dashboard.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/courseBuilder.css?v=4" rel="stylesheet" type="text/css" />
    <link href="../../Content/messages.css?v=1" rel="stylesheet" type="text/css" />
    <style>
        /* General text styling updates for clean black text and no blue underlines */
        body, .page-subtitle, .fm-stat, .fm-body, .fm-title, h3 {
            color: #111827 !important;
        }

        a, a:hover, a:focus, a:active {
            text-decoration: none !important;
        }

        .fm-replies-container {
            margin-top: 12px;
            padding-top: 12px;
            border-top: 1px solid #e2e8f0;
            display: none;
        }
        .fm-reply-item {
            background: #f8fafc;
            padding: 10px 14px;
            border-radius: 6px;
            margin-bottom: 8px;
            font-size: 13px;
            color: #111827;
        }
        .fm-reply-author {
            font-weight: 600;
            color: #111827;
            display: flex;
            justify-content: space-between;
            margin-bottom: 4px;
        }
        .fm-inline-reply-box {
            margin-top: 12px;
            display: flex;
            flex-direction: column;
            gap: 8px;
        }
        .filter-btn-badge {
            background: none;
            border: none;
            cursor: pointer;
            padding: 0;
            font: inherit;
            display: inline-flex;
            align-items: center;
            color: #111827;
            text-decoration: none !important;
        }
        .filter-btn-badge:hover {
            opacity: 0.8;
            color: #000000;
            text-decoration: none !important;
        }
        .fm-title {
            font-weight: 600;
            font-size: 16px;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="dashboard-layout">

            <% Server.Execute("tutorNav.aspx", false); %>

            <main class="dashboard-container">

                <div class="page-header">
                    <div>
                        <h1 class="page-title" style="color: #111827;">Communications Desk</h1>
                        <p class="page-subtitle">Moderate student inquiries and configure announcements.</p>
                    </div>
                </div>

                <div class="fm-ttabs">
                    <a href="messages.aspx" class="fm-ttab active">Discussion Moderation</a>
                    <a href="calendar.aspx" class="fm-ttab">My Calendar</a>
                </div>

                <asp:Label ID="LblNote" runat="server" CssClass="fm-note" Visible="false" />

                <!-- Toolbar with course filter, reset filter, and interactive unanswered filter button -->
                <div class="fm-toolbar" style="display: flex; align-items: center; gap: 12px; flex-wrap: wrap;">
                    <asp:DropDownList ID="DdlCourse" runat="server" CssClass="fm-course-select" AutoPostBack="true" OnSelectedIndexChanged="DdlCourse_SelectedIndexChanged" />
                    <asp:Button ID="BtnResetFilter" runat="server" Text="Show All Topics" CssClass="btn btn-outline btn-sm" OnClick="BtnResetFilter_Click" />
                    
                    <div style="margin-left: auto; display: flex; gap: 15px; align-items: center;">
                        <span class="fm-stat">Active Threads: <b><%= ActiveThreads %></b></span>
                        <span class="fm-stat">
                            <asp:LinkButton ID="BtnFilterUnanswered" runat="server" OnClick="BtnFilterUnanswered_Click" CssClass="filter-btn-badge" ToolTip="Click to filter unanswered questions">
                                Unanswered: <span class="pill warn" style="margin-left: 4px;"><%= UnansweredCount %></span>
                            </asp:LinkButton>
                        </span>
                    </div>
                </div>

                <div class="fm-layout wide">

                    <!-- Left: threads -->
                    <div>
                        <asp:Repeater ID="RptThreads" runat="server" OnItemCommand="RptThreads_ItemCommand">
                            <ItemTemplate>
                                <div class='fm-card fm-thread'>
                                    <div class="fm-author">
                                        <span class='fm-av fm-c<%# Eval("ColorIndex") %>'><%#: Eval("Initials") %></span>
                                        <div class="fm-who">
                                            <strong style="color: #111827;"><%#: Eval("Author") %></strong>
                                            <small>&bull; <%#: Eval("When") %></small>
                                        </div>
                                        <span class='fm-status <%# Eval("StatusClass") %>'><%#: Eval("StatusLabel") %></span>
                                    </div>
                                    <h3 class="fm-title"><%#: Eval("Title") %></h3>
                                    <p class="fm-body"><%#: Eval("Excerpt") %></p>
                                    
                                    <div class="fm-actions">
                                        <button type="button" class="fm-act" onclick="toggleReplies('replies-<%# Eval("ThreadId") %>')">
                                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"></path></svg>
                                            View/Hide Replies & Reply (<%#: Eval("RepliesText") %>)
                                        </button>
                                    </div>

                                    <!-- Expandable Replies Section with Inline Reply Box -->
                                    <div id="replies-<%# Eval("ThreadId") %>" class="fm-replies-container">
                                        <asp:Repeater ID="RptReplies" runat="server" DataSource='<%# Eval("ReplyList") %>'>
                                            <ItemTemplate>
                                                <div class="fm-reply-item">
                                                    <div class="fm-reply-author">
                                                        <span><strong><%#: Eval("Author") %></strong></span>
                                                        <small><%#: Eval("When") %></small>
                                                    </div>
                                                    <div><%#: Eval("Content") %></div>
                                                </div>
                                            </ItemTemplate>
                                        </asp:Repeater>

                                        <!-- Inline Reply Form -->
                                        <div class="fm-inline-reply-box">
                                            <asp:TextBox ID="TxtInlineReply" runat="server" TextMode="MultiLine" Rows="3" CssClass="form-control" placeholder="Write a reply to this thread..." />
                                            <asp:Button ID="BtnSubmitInlineReply" runat="server" Text="Post Reply" CommandName="PostInlineReply" CommandArgument='<%# Eval("ThreadId") %>' CssClass="fm-btn btn-sm" style="align-self: flex-end;" />
                                        </div>
                                    </div>

                                </div>
                            </ItemTemplate>
                        </asp:Repeater>

                        <asp:Panel ID="PnlNoThreads" runat="server" CssClass="fm-empty-box" Visible="false">
                            No discussions to moderate yet.
                        </asp:Panel>
                    </div>

                    <!-- Right: Announcement section -->
                    <div>
                        <div class="fm-box tutor">
                            <h3>Post Announcement</h3>

                            <label for="DdlAnnounceCourse">Target Course (Includes Category)</label>
                            <asp:DropDownList ID="DdlAnnounceCourse" runat="server" CssClass="form-control" AutoPostBack="true" OnSelectedIndexChanged="DdlAnnounceCourse_SelectedIndexChanged" style="margin-bottom: 10px;" />

                            <label for="DdlAnnounceLesson">Target Lesson (Optional)</label>
                            <asp:DropDownList ID="DdlAnnounceLesson" runat="server" CssClass="form-control" style="margin-bottom: 10px;">
                                <asp:ListItem Value="" Text="Select Lesson (Optional)" />
                            </asp:DropDownList>

                            <label for="TxtAnnounceTitle">Announcement Title</label>
                            <asp:TextBox ID="TxtAnnounceTitle" runat="server" MaxLength="150" placeholder="Enter title e.g. Midterm Date Set" />

                            <label for="TxtAnnounceBody">Message</label>
                            <asp:TextBox ID="TxtAnnounceBody" runat="server" TextMode="MultiLine" Rows="4" placeholder="Write global notice here..." />

                            <asp:Button ID="BtnPostAnnouncement" runat="server" Text="Post Announcement" CssClass="fm-btn block" OnClick="BtnPostAnnouncement_Click" />
                        </div>
                    </div>

                </div>
            </main>
        </div>
    </form>

    <script>
        function toggleReplies(elementId) {
            var el = document.getElementById(elementId);
            if (el) {
                el.style.display = (el.style.display === 'block') ? 'none' : 'block';
            }
        }

        var navLink = document.querySelector('.nav-link[href="messages.aspx"]');
        if (navLink) navLink.classList.add('active');
    </script>
</body>
</html>