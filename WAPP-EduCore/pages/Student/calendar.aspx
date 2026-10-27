<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="calendar.aspx.cs" Inherits="WAPP_EduCore.pages.Student.calendar" %>
<%@ Register Src="~/pages/Shared/Sidebar.ascx" TagName="Sidebar" TagPrefix="uc" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Calendar</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Merriweather:wght@400;700&display=swap" rel="stylesheet" />
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/student.css" rel="stylesheet" type="text/css" />
    <link href="/Content/student-upgrade.css" rel="stylesheet" />
    <script src="/Content/fx.js"></script>
    <style>
        .cl-head-left{display:flex;align-items:center;gap:14px}
        .cl-nav{display:flex;gap:6px}
        .cl-nav a{display:flex;align-items:center;justify-content:center;width:28px;height:28px;border-radius:6px;background:#f1f0ee;color:#111c3d;font-weight:700;text-decoration:none}
        .cl-today{padding:6px 12px;border-radius:6px;background:#f1f0ee;color:#111c3d;font-size:13px;font-weight:600;text-decoration:none}
        .cl-views{display:flex;gap:8px}
        .cl-views a{padding:8px 16px;border-radius:6px;background:#f1f0ee;color:#4a5468;font-size:13px;font-weight:600;text-decoration:none}
        .cl-views a.active{background:#243f86;color:#fff}

        .cl-wrap{display:grid;grid-template-columns:1fr 320px;margin:0 -40px -40px;min-height:calc(100vh - 82px)}
        .cl-main{min-width:0;padding:24px 32px;overflow-x:auto}
        .cl-side{padding:20px;background:#fff;border-left:1px solid #e8e8e8}
        .cl-empty{padding:30px 10px;text-align:center;font-size:13px;color:#7b8697}

        /* week and day grid */
        .cl-grid{display:grid;grid-template-columns:60px repeat(var(--n),minmax(90px,1fr));gap:6px;min-width:560px}
        .cl-dayhead{padding:8px 0;border:1px solid #e3e3e3;border-radius:8px;background:#fff;color:#16223b;font-size:13px;font-weight:600;text-align:center;text-decoration:none}
        .cl-dayhead.today{background:#243f86;border-color:#243f86;color:#fff}
        .cl-times{display:flex;flex-direction:column;gap:6px}
        .cl-time{display:flex;align-items:center;justify-content:flex-end;height:64px;padding-right:8px;font-size:12px;color:#9aa6b8}
        .cl-col{position:relative;display:flex;flex-direction:column;gap:6px}
        .cl-slot{height:64px;background:#fff;border:1px solid #ebe9e6;border-radius:8px}
        .cl-ev{position:absolute;box-sizing:border-box;padding:6px 8px;overflow:hidden;border-radius:6px;color:#fff;font-size:12px;line-height:1.25}
        .cl-ev strong{display:block;font-weight:600}
        .cl-ev small{display:block;opacity:.85;font-size:11px}

        .ev-live{background:#243f86}
        .ev-deadline{background:#e0a85a;color:#2b2b2b}
        .ev-exam{background:#c05a5a}
        .ev-study{background:#5068c8}
        .ev-other{background:#6b7385}

        /* month grid */
        .cl-month{display:grid;grid-template-columns:repeat(7,minmax(80px,1fr));gap:6px}
        .cl-mhead{padding:8px;font-size:12px;font-weight:700;color:#7b8697;text-align:center}
        .cl-mcell{min-height:104px;padding:6px;background:#fff;border:1px solid #ebe9e6;border-radius:8px;font-size:12px}
        .cl-mcell.muted{background:#faf9f7}
        .cl-mcell.muted .cl-mnum{color:#b3b7c0}
        .cl-mcell.today{border-color:#243f86;box-shadow:inset 0 0 0 1px #243f86}
        .cl-mnum{display:inline-block;margin-bottom:4px;font-weight:600;color:#111c3d;text-decoration:none}
        .cl-chip{margin-bottom:3px;padding:2px 6px;overflow:hidden;border-radius:4px;color:#fff;font-size:11px;white-space:nowrap;text-overflow:ellipsis}
        .cl-more{font-size:11px;color:#7b8697}

        /* right panel */
        .cl-up{padding:20px;background:#fbead6;border-radius:14px}
        .cl-up h3{margin:0 0 14px;font-family:'Merriweather',Georgia,serif;font-size:19px;color:#111c3d}
        .cl-item{position:relative;padding:12px 14px;margin-bottom:12px;background:#fff;border-radius:10px}
        .cl-tag{display:inline-block;margin-bottom:6px;padding:2px 7px;border-radius:4px;color:#fff;font-size:11px;font-weight:600}
        .cl-itemtitle{font-family:'Merriweather',Georgia,serif;font-size:15px;font-weight:700;color:#111c3d}
        .cl-itemdate{margin-top:4px;font-size:12px;color:#7b8697}
        .cl-del{position:absolute;top:8px;right:10px;font-size:11px;color:#9aa0ad;text-decoration:none}
        .cl-del:hover{color:#c05a5a}
        .cl-add{display:block;margin-top:16px;padding:14px;border-radius:8px;background:#243f86;color:#fff;font-size:14px;font-weight:600;text-align:center;text-decoration:none}
        .cl-add:hover{background:#1b3270}

        /* add-event popup (modal styles come from admin.css) */
        .cl-btn-solid{padding:10px 20px;border:0;border-radius:8px;background:#243f86;color:#fff;font-size:14px;font-weight:600;cursor:pointer}
        .cl-btn-solid:hover{background:#1b3270}
        .cl-btn-ghost{padding:10px 20px;border:1px solid #e0e0e0;border-radius:8px;background:#fff;color:#16223b;font-size:14px;cursor:pointer}
        .cl-close{border:0;background:none;font-size:22px;line-height:1;color:#7b8697;cursor:pointer}
        .cl-times-row{display:grid;grid-template-columns:1fr 1fr;gap:12px}

        @media(max-width:1000px){.cl-wrap{grid-template-columns:1fr}.cl-side{border-left:0;border-top:1px solid #e8e8e8}}
    </style>
    <script>
function openAdd() { document.getElementById('addModal').classList.add('open'); }
function closeAdd() { document.getElementById('addModal').classList.remove('open'); }
</script>
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <uc:Sidebar ID="Sidebar1" runat="server" />

            <main class="main">
                <div class="topbar">
                    <div class="cl-head-left">
                        <span class="title"><asp:Literal ID="litTitle" runat="server" Mode="Encode"></asp:Literal></span>
                        <asp:Literal ID="litNav" runat="server" Mode="PassThrough"></asp:Literal>
                    </div>
                    <div class="cl-views">
                        <asp:Literal ID="litViews" runat="server" Mode="PassThrough"></asp:Literal>
                    </div>
                </div>

                <div class="cl-wrap">
                    <div class="cl-main">
                        <asp:Literal ID="litGrid" runat="server" Mode="PassThrough"></asp:Literal>
                    </div>

                    <div class="cl-side">
                        <div class="cl-up">
                            <h3>Upcoming Events</h3>
                            <asp:Panel ID="pnlNoUpcoming" runat="server" Visible="false" CssClass="cl-empty">
                                No upcoming events yet. Use Add Event to plan something.
                            </asp:Panel>
                            <asp:Repeater ID="rptUpcoming" runat="server" OnItemCommand="rptUpcoming_ItemCommand">
                                <ItemTemplate>
                                    <div class="cl-item">
                                        <asp:LinkButton runat="server" CssClass="cl-del" CommandName="delete"
                                            CommandArgument='<%# Eval("Id") %>'
                                            OnClientClick="return confirm('Remove this event?');">Remove</asp:LinkButton>
                                        <span class='<%# "cl-tag " + Eval("Css") %>'><%#: Eval("Type") %></span>
                                        <div class="cl-itemtitle"><%#: Eval("Title") %></div>
                                        <div class="cl-itemdate"><%#: Eval("When") %></div>
                                    </div>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                        <% if (Session["UserID"] == null) { %>
                        <a href="#" class="cl-add" onclick="openLoginPrompt(); return false;">+ Add Event</a>
                        <% } else { %>
                        <a href="#" class="cl-add" onclick="openAdd(); return false;">+ Add Event</a>
                        <% } %>
                    </div>
                </div>
            </main>
        </div>

        <!-- Add Event popup -->
        <div class="modal-back" id="addModal">
            <div class="modal-box">
                <div class="modal-head">
                    <h3>Add Event</h3>
                    <button type="button" class="cl-close" onclick="closeAdd()">&times;</button>
                </div>

                <asp:Label ID="lblFormError" runat="server" CssClass="form-error" Visible="false"></asp:Label>

                <div class="form-field">
                    <label for="txtTitle">Title</label>
                    <asp:TextBox ID="txtTitle" runat="server" CssClass="input" MaxLength="120"></asp:TextBox>
                </div>
                <div class="form-field">
                    <label for="ddlType">Type</label>
                    <asp:DropDownList ID="ddlType" runat="server" CssClass="input">
                        <asp:ListItem Value="Live Class">Live Class</asp:ListItem>
                        <asp:ListItem Value="Deadline">Deadline</asp:ListItem>
                        <asp:ListItem Value="Exam">Exam</asp:ListItem>
                        <asp:ListItem Value="Self-Study">Self-Study</asp:ListItem>
                        <asp:ListItem Value="Other" Selected="True">Other</asp:ListItem>
                    </asp:DropDownList>
                </div>
                <div class="form-field">
                    <label for="ddlCourse">Course (optional)</label>
                    <asp:DropDownList ID="ddlCourse" runat="server" CssClass="input"></asp:DropDownList>
                </div>
                <div class="form-field">
                    <label for="txtDate">Date</label>
                    <asp:TextBox ID="txtDate" runat="server" TextMode="Date" CssClass="input"></asp:TextBox>
                </div>
                <div class="cl-times-row">
                    <div class="form-field">
                        <label for="txtStart">Start time</label>
                        <asp:TextBox ID="txtStart" runat="server" TextMode="Time" CssClass="input"></asp:TextBox>
                    </div>
                    <div class="form-field">
                        <label for="txtEnd">End time</label>
                        <asp:TextBox ID="txtEnd" runat="server" TextMode="Time" CssClass="input"></asp:TextBox>
                    </div>
                </div>

                <div class="modal-buttons" style="margin-top:6px">
                    <asp:Button ID="btnAdd" runat="server" Text="Save Event" CssClass="cl-btn-solid" OnClick="btnAdd_Click" />
                    <button type="button" class="cl-btn-ghost" onclick="closeAdd()">Cancel</button>
                </div>
            </div>
        </div>

        <!-- Not Logged In Modal Popup -->
<div class="modal-back" id="loginPromptModal">
    <div class="modal-box" style="text-align: center; padding: 30px 24px;">
        <div style="font-size: 40px; margin-bottom: 12px;">🔒</div>
        <h3 style="font-family: 'Merriweather', Georgia, serif; font-size: 20px; color: #111c3d; margin-bottom: 8px;">Not logged in?</h3>
        <p style="font-size: 14px; color: #4a5468; margin-bottom: 24px; line-height: 1.5;">
            Login or create an account to unlock all functions, post discussions, track your progress, and manage your schedule!
        </p>
        <div style="display: flex; gap: 12px; justify-content: center;">
            <a href="<%= ResolveUrl("~/auth/login.aspx") %>" class="cl-btn-solid" style="text-decoration: none; padding: 10px 24px;">Log In</a>
            <a href="<%= ResolveUrl("~/auth/register.aspx") %>" class="cl-btn-ghost" style="text-decoration: none; padding: 10px 24px; display: inline-flex; align-items: center;">Sign Up</a>
            <button type="button" class="cl-btn-ghost" onclick="closeLoginPrompt()" style="padding: 10px 16px;">Cancel</button>
        </div>
    </div>
</div>

<script>
function openLoginPrompt() {
    document.getElementById('loginPromptModal').classList.add('open');
}
function closeLoginPrompt() {
    document.getElementById('loginPromptModal').classList.remove('open');
}
</script>

    </form>
</body>
</html>