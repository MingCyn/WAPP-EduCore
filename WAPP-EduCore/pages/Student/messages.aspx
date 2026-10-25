<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="messages.aspx.cs" Inherits="WAPP_EduCore.pages.Student.messages" %>
<%@ Register Src="~/pages/Shared/Sidebar.ascx" TagName="Sidebar" TagPrefix="uc" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Discussion Forum</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Merriweather:wght@400;700&display=swap" rel="stylesheet" />
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/student.css" rel="stylesheet" type="text/css" />
    <%-- The forum styles that used to be inline here now live in the shared messages.css (also used by Tutor/messages.aspx) --%>
    <link href="../../Content/messages.css" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <uc:Sidebar ID="Sidebar1" runat="server" />

            <main class="main">
                <div class="topbar">
                    <div class="fm-head-left">
                        <span class="title">Discussion Forum</span>
                        <asp:Literal ID="litChip" runat="server" Mode="PassThrough"></asp:Literal>
                    </div>
                    <div class="fm-head-right">
                        <asp:Literal ID="litTopic" runat="server" Mode="PassThrough"></asp:Literal>
                    </div>
                </div>

                <asp:Panel ID="pnlNoCourses" runat="server" Visible="false" CssClass="fm-empty-box">
                    You are not enrolled in any course yet, so there are no discussions to show.
                    <br /><a href="myCourses.aspx">Browse courses</a>
                </asp:Panel>

                <asp:Panel ID="pnlForum" runat="server">
                    <div class="fm-tabs">
                        <asp:Literal ID="litTabs" runat="server" Mode="PassThrough"></asp:Literal>
                    </div>

                    <div class="fm-layout">
                        <div>
                            <asp:Literal ID="litBefore" runat="server" Mode="PassThrough"></asp:Literal>

                            <asp:Panel ID="pnlSelected" runat="server" Visible="false" CssClass="fm-card selected">
                                <asp:Literal ID="litSelTop" runat="server" Mode="PassThrough"></asp:Literal>
                                <div class="fm-replies">
                                    <asp:Literal ID="litReplies" runat="server" Mode="PassThrough"></asp:Literal>
                                </div>
                                <asp:Label ID="lblReplyError" runat="server" CssClass="fm-error" Visible="false"></asp:Label>
                                <div class="fm-replyform">
                                    <asp:TextBox ID="txtReply" runat="server" TextMode="MultiLine" Rows="2"
                                        placeholder="Write a reply..."></asp:TextBox>
                                    <asp:Button ID="btnReply" runat="server" Text="Reply" CssClass="fm-btn small"
                                        OnClick="btnReply_Click" />
                                </div>
                            </asp:Panel>

                            <asp:Literal ID="litAfter" runat="server" Mode="PassThrough"></asp:Literal>

                            <asp:Panel ID="pnlNoPosts" runat="server" Visible="false" CssClass="fm-empty-box">
                                No discussions here yet. Start one using the form.
                            </asp:Panel>
                        </div>

                        <div>
                            <div class="fm-box">
                                <h3>Post New Discussion</h3>
                                <asp:Label ID="lblPostError" runat="server" CssClass="fm-error" Visible="false"></asp:Label>

                                <label for="txtTitle">Topic title</label>
                                <asp:TextBox ID="txtTitle" runat="server" MaxLength="150" placeholder="Enter thread title..."></asp:TextBox>

                                <asp:Panel ID="pnlCoursePick" runat="server">
                                    <label for="ddlCourse">Course</label>
                                    <asp:DropDownList ID="ddlCourse" runat="server"></asp:DropDownList>
                                </asp:Panel>

                                <label for="ddlType">Type</label>
                                <asp:DropDownList ID="ddlType" runat="server">
                                    <asp:ListItem Value="Discussion">Discussion</asp:ListItem>
                                    <asp:ListItem Value="Question">Question</asp:ListItem>
                                </asp:DropDownList>

                                <label for="txtContent">Content</label>
                                <asp:TextBox ID="txtContent" runat="server" TextMode="MultiLine" Rows="4"
                                    placeholder="Explain details clearly..."></asp:TextBox>

                                <asp:Button ID="btnPost" runat="server" Text="Post Topic" CssClass="fm-btn" OnClick="btnPost_Click" />
                            </div>

                            <div class="fm-box">
                                <h3>Active Participants</h3>
                                <asp:Literal ID="litPeople" runat="server" Mode="PassThrough"></asp:Literal>
                            </div>
                        </div>
                    </div>
                </asp:Panel>
            </main>
        </div>

        <!-- Not Logged In Modal Popup -->
        <div class="modal-back" id="loginPromptModal">
            <div class="modal-box" style="text-align: center; padding: 32px; max-width: 440px;">
                <div style="font-size: 40px; margin-bottom: 12px;">🔒</div>
                <h3 style="font-family: 'Merriweather', Georgia, serif; font-size: 20px; color: #111c3d; margin-bottom: 8px;">Not logged in?</h3>
                <p style="font-size: 14px; color: #4a5468; margin-bottom: 24px; line-height: 1.5;">
                    Login or create an account to unlock all functions, post discussions, track your progress, and manage your schedule!
                </p>
                <div style="display: flex; gap: 12px; justify-content: center;">
                    <a href="<%= ResolveUrl("~/Auth/login.aspx") %>" class="cl-btn-solid" style="text-decoration: none; padding: 10px 24px; display: inline-flex; align-items: center; justify-content: center;">Log In</a>
                    <a href="<%= ResolveUrl("~/Auth/register.aspx") %>" class="cl-btn-ghost" style="text-decoration: none; padding: 10px 24px; display: inline-flex; align-items: center; justify-content: center;">Sign Up</a>
                    <button type="button" class="cl-btn-ghost" onclick="closeLoginPrompt()" style="padding: 10px 16px;">Cancel</button>
                </div>
            </div>
        </div>

        <script>
        if (typeof openLoginPrompt !== 'function') {
            window.openLoginPrompt = function () {
                var modal = document.getElementById('loginPromptModal');
                if (modal) modal.classList.add('open');
            };
        }

        if (typeof closeLoginPrompt !== 'function') {
            window.closeLoginPrompt = function () {
                var modal = document.getElementById('loginPromptModal');
                if (modal) modal.classList.remove('open');
            };
        }
        </script>

    </form>
</body>
</html>
