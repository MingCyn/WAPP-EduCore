<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="messages.aspx.cs" Inherits="WAPP_EduCore.pages.Tutor.messages" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>EduCore - Communications Desk</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Source+Serif+4:wght@600;700&display=swap" rel="stylesheet" />
    <link href="../../Content/dashboard.css" rel="stylesheet" type="text/css" />
    <%-- courseBuilder.css supplies the shared nav tweaks, page header and .btn styles --%>
    <link href="../../Content/courseBuilder.css?v=4" rel="stylesheet" type="text/css" />
    <%-- Forum styles shared with Student/messages.aspx --%>
    <link href="../../Content/messages.css?v=1" rel="stylesheet" type="text/css" />
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
                        <h1 class="page-title">Communications Desk</h1>
                        <p class="page-subtitle">Moderate student inquiries and configure announcements.</p>
                    </div>
                </div>

                <!-- Tabs -->
                <div class="fm-ttabs">
                    <a href="messages.aspx" class="fm-ttab active">Discussion Moderation</a>
                    <a href="calendar.aspx" class="fm-ttab">My Calendar</a>
                </div>

                <asp:Label ID="LblNote" runat="server" CssClass="fm-note" Visible="false" />

                <!-- Course picker + counters -->
                <div class="fm-toolbar">
                    <asp:DropDownList ID="DdlCourse" runat="server" CssClass="fm-course-select" AutoPostBack="true" OnSelectedIndexChanged="DdlCourse_SelectedIndexChanged" />
                    <span class="fm-stat">Active Threads: <b><%= ActiveThreads %></b></span>
                    <span class="fm-stat">Unanswered: <span class="pill warn"><%= UnansweredCount %></span></span>
                    <span class="fm-stat">Flagged: <span class="pill bad"><%= FlaggedCount %></span></span>
                </div>

                <div class="fm-layout wide">

                    <!-- Left: threads -->
                    <div>
                        <asp:Repeater ID="RptThreads" runat="server">
                            <ItemTemplate>
                                <div class='fm-card fm-thread<%# (bool)Eval("IsFlagged") ? " is-flagged" : "" %>'>
                                    <div class="fm-author">
                                        <span class='fm-av fm-c<%# Eval("ColorIndex") %>'><%#: Eval("Initials") %></span>
                                        <div class="fm-who">
                                            <strong><%#: Eval("Author") %></strong>
                                            <small>&bull; <%#: Eval("When") %></small>
                                        </div>
                                        <span class='fm-status <%# Eval("StatusClass") %>'><%#: Eval("StatusLabel") %></span>
                                    </div>
                                    <h3 class="fm-title"><%#: Eval("Title") %></h3>
                                    <p class="fm-body"><%#: Eval("Excerpt") %></p>
                                    <div class="fm-actions">
                                        <button type="button" class="fm-act primary" onclick="replyTo('<%# Eval("ThreadId") %>')">
                                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M9 14L4 9l5-5" /><path d="M4 9h10a6 6 0 0 1 6 6v3" /></svg>
                                            Reply
                                        </button>
                                        <button type="button" class="fm-act" title="Pin this thread to the top">
                                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 17v5" /><path d="M9 3h6l-1 6 3 3v2H7v-2l3-3z" /></svg>
                                            Pin Thread
                                        </button>
                                        <span class="fm-count">
                                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 12a8 8 0 0 1-11.6 7.1L4 20l1-4.6A8 8 0 1 1 21 12z" /></svg>
                                            <%#: Eval("RepliesText") %>
                                        </span>
                                    </div>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>

                        <asp:Panel ID="PnlNoThreads" runat="server" CssClass="fm-empty-box" Visible="false">
                            No discussions to moderate yet.
                        </asp:Panel>
                    </div>

                    <!-- Right: quick reply, announcement, moderation rules -->
                    <div>
                        <div class="fm-box tutor">
                            <h3>Quick Reply</h3>
                            <label for="DdlThread">Target Thread</label>
                            <asp:DropDownList ID="DdlThread" runat="server" />

                            <label for="TxtReply">Reply Content</label>
                            <asp:TextBox ID="TxtReply" runat="server" TextMode="MultiLine" Rows="4" placeholder="Type your educational tips or guide response here..." />

                            <asp:Button ID="BtnPostReply" runat="server" Text="Post Reply" CssClass="fm-btn block" OnClick="BtnPostReply_Click" />
                        </div>

                        <div class="fm-box tutor">
                            <h3>Post Announcement</h3>
                            <label for="TxtAnnounceTitle">Announcement Title</label>
                            <asp:TextBox ID="TxtAnnounceTitle" runat="server" MaxLength="150" placeholder="Enter title e.g. Midterm Date Set" />

                            <label for="TxtAnnounceBody">Message</label>
                            <asp:TextBox ID="TxtAnnounceBody" runat="server" TextMode="MultiLine" Rows="4" placeholder="Write global notice here..." />

                            <asp:Button ID="BtnPostAnnouncement" runat="server" Text="Post Announcement" CssClass="fm-btn block" OnClick="BtnPostAnnouncement_Click" />
                        </div>

                        <div class="fm-box tutor">
                            <h3>Moderation Rules</h3>
                            <div class="fm-switch-row">
                                <span>Flag Inappropriate Content</span>
                                <label class="fm-switch"><input type="checkbox" id="ChkFlagContent" runat="server" checked="checked" /><span class="fm-slider"></span></label>
                            </div>
                            <div class="fm-switch-row">
                                <span>Require First Post Approval</span>
                                <label class="fm-switch"><input type="checkbox" id="ChkApproveFirst" runat="server" /><span class="fm-slider"></span></label>
                            </div>
                        </div>
                    </div>

                </div>
            </main>
        </div>
    </form>

    <script>
/** Reply button on a thread card: pick that thread in Quick Reply and jump to the text box. @param {string} id */
function replyTo(id) {
    var ddl = /** @type {HTMLSelectElement | null} */ (document.getElementById('DdlThread'));
    var box = /** @type {HTMLTextAreaElement | null} */ (document.getElementById('TxtReply'));
    if (ddl) ddl.value = id;
    if (box) {
        box.scrollIntoView({ behavior: 'smooth', block: 'center' });
        box.focus();
    }
}

// Highlight "Messages" in the shared nav
var navLink = document.querySelector('.nav-link[href="messages.aspx"]');
if (navLink) navLink.classList.add('active');
</script>
</body>
</html>
