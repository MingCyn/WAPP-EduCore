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
    <style>
        .fm-head-left{display:flex;align-items:center;gap:14px}
        .fm-chip{padding:5px 10px;border-radius:6px;background:#fbead6;color:#243f86;font-size:12px;font-weight:600}
        .fm-head-right{display:flex;align-items:center;gap:16px;font-size:13px;color:#7b8697}
        .fm-back{padding:7px 14px;border:1px solid #e0e0e0;border-radius:6px;background:#fff;color:#16223b;font-size:13px;text-decoration:none}

        .fm-tabs{display:flex;flex-wrap:wrap;gap:10px;margin-bottom:24px}
        .fm-tab{padding:9px 18px;border:1px solid #e3e3e3;border-radius:999px;background:#fff;color:#16223b;font-size:14px;text-decoration:none}
        .fm-tab:hover{border-color:#243f86}
        .fm-tab.active{background:#243f86;border-color:#243f86;color:#fff;font-weight:600}

        .fm-layout{display:grid;grid-template-columns:1fr 340px;gap:24px;align-items:start}
        .fm-card{display:block;padding:18px 22px;margin-bottom:16px;background:#fff;border:1px solid #ebe7e1;border-radius:12px;color:inherit;text-decoration:none}
        a.fm-card:hover{border-color:#4f67cc}
        .fm-card.selected{border:2px solid #4f67cc}
        .fm-author{display:flex;align-items:center;gap:12px;margin-bottom:12px}
        .fm-who strong{display:block;font-size:14px;color:#111c3d}
        .fm-who small{font-size:12px;color:#8a8f9c}
        .fm-tag{margin-left:auto;padding:2px 8px;border-radius:4px;font-size:11px;font-weight:600;color:#fff}
        .fm-tag.q{background:#5068c8}
        .fm-tag.a{background:#e0a85a;color:#2b2b2b}
        .fm-title{margin-bottom:8px;font-family:'Merriweather',Georgia,serif;font-size:17px;font-weight:700;color:#111c3d}
        .fm-body{margin-bottom:14px;font-size:14px;line-height:1.6;color:#4a5468}
        .fm-meta{font-size:12px;color:#8a8f9c}

        .fm-replies{margin:6px 0 14px 4px;padding-left:14px;border-left:2px solid #ebe7e1}
        .fm-reply{padding:10px 14px;margin-bottom:10px;background:#f8f7f5;border-radius:8px;font-size:13px;line-height:1.5;color:#4a5468}
        .fm-reply strong{display:block;margin-bottom:2px;color:#111c3d}
        .fm-noreplies{font-size:13px;color:#8a8f9c}
        .fm-replyform{display:flex;gap:10px;align-items:flex-end}
        .fm-replyform textarea{flex:1;padding:10px 12px;border:1px solid #e3e3e3;border-radius:8px;font-family:inherit;font-size:13px;resize:vertical;box-sizing:border-box}

        .fm-box{padding:22px 24px;margin-bottom:20px;background:#fff;border:1px solid #ebe7e1;border-radius:12px}
        .fm-box h3{margin:0 0 6px;font-family:'Merriweather',Georgia,serif;font-size:18px;color:#111c3d}
        .fm-box label{display:block;margin:12px 0 6px;font-size:12px;letter-spacing:.3px;text-transform:uppercase;color:#7b8697}
        .fm-box input[type=text],.fm-box select,.fm-box textarea{width:100%;padding:10px 12px;border:1px solid #e3e3e3;border-radius:8px;background:#fbfbfb;font-family:inherit;font-size:14px;box-sizing:border-box}
        .fm-box textarea{min-height:90px;resize:vertical}
        .fm-box input:focus,.fm-box select:focus,.fm-box textarea:focus,.fm-replyform textarea:focus{outline:none;border-color:#4f67cc}
        .fm-btn{margin-top:16px;padding:10px 20px;border:0;border-radius:8px;background:#243f86;color:#fff;font-size:14px;font-weight:600;cursor:pointer}
        .fm-btn:hover{background:#1b3270}
        .fm-btn.small{margin-top:0;padding:9px 16px}
        .fm-error{display:block;margin:8px 0;font-size:12px;color:#d64545}

        .fm-people{display:flex;flex-wrap:wrap;gap:10px;margin-top:14px}
        .fm-av{display:inline-flex;align-items:center;justify-content:center;flex-shrink:0;width:32px;height:32px;border-radius:50%;color:#fff;font-size:12px;font-weight:700}
        .fm-c0{background:#243f86}
        .fm-c1{background:#5068c8}
        .fm-c2{background:#2e8b4f}
        .fm-c3{background:#c05a5a}
        .fm-empty-box{padding:36px 20px;text-align:center;background:#fff;border:1px solid #ebe7e1;border-radius:12px;font-size:14px;color:#7b8697}

        @media(max-width:1000px){.fm-layout{grid-template-columns:1fr}}

        /* ===== Modal Action Buttons Style ===== */
        .cl-btn-solid {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            padding: 10px 24px;
            border: 0;
            border-radius: 8px;
            background: #243f86;
            color: #fff;
            font-size: 14px;
            font-weight: 600;
            text-decoration: none;
            cursor: pointer;
            transition: background 0.2s ease;
        }

        .cl-btn-solid:hover {
            background: #1b3270;
            color: #fff;
        }

        .cl-btn-ghost {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            padding: 10px 24px;
            border: 1px solid #e3e3e3;
            border-radius: 8px;
            background: #fff;
            color: #16223b;
            font-size: 14px;
            font-weight: 600;
            text-decoration: none;
            cursor: pointer;
            transition: border-color 0.2s ease, background 0.2s ease;
        }

        .cl-btn-ghost:hover {
            border-color: #243f86;
            background: #f8f7f5;
            color: #111c3d;
        }

    </style>
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