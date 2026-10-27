<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="certificates.aspx.cs" Inherits="WAPP_EduCore.pages.Student.certificates" %>
<%@ Register Src="~/pages/Shared/Sidebar.ascx" TagName="Sidebar" TagPrefix="uc" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Certificates</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Merriweather:wght@400;700&display=swap" rel="stylesheet" />
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/student.css" rel="stylesheet" type="text/css" />
    <link href="/Content/student-upgrade.css" rel="stylesheet" />
    <script src="/Content/fx.js"></script>
    <style>
        .cf-summary{margin:0 0 4px;font-size:14px;color:#6b7385}
        .cf-h{margin:28px 0 14px;font-family:'Merriweather',Georgia,serif;font-size:20px;color:#111c3d}
        .cf-grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(300px,1fr));gap:20px}
        .cf-card{padding:20px 22px;background:#fff;border:1px solid #ebe7e1;border-radius:12px}
        .cf-card.ready{background:#fbead6;border-color:#efdcc3}
        .cf-badge{display:inline-block;margin-bottom:10px;padding:3px 9px;border-radius:4px;background:#e8f5ec;color:#1f5f38;font-size:11px;font-weight:700}
        .cf-card.ready .cf-badge{background:#243f86;color:#fff}
        .cf-card.prog .cf-badge{background:#f1f0ee;color:#6b7385}
        .cf-title{margin-bottom:6px;font-family:'Merriweather',Georgia,serif;font-size:17px;font-weight:700;line-height:1.3;color:#111c3d}
        .cf-meta{margin-bottom:4px;font-size:13px;color:#6b7385}
        .cf-code{margin:10px 0 0;font-size:12px;letter-spacing:.5px;color:#4a5468}
        .cf-bar{height:8px;margin:12px 0 6px;background:#ecebe8;border-radius:6px}
        .cf-bar span{display:block;height:100%;background:#4f67cc;border-radius:6px}
        .cf-empty{padding:40px 10px;text-align:center;font-size:14px;color:#7b8697}

        .cf-solid{display:inline-block;padding:10px 20px;border:0;border-radius:8px;background:#243f86;color:#fff;font-size:14px;font-weight:600;text-align:center;text-decoration:none;cursor:pointer}
        .cf-solid:hover{background:#1b3270}
        .cf-solid.full{display:block;width:100%;margin-top:14px;box-sizing:border-box}
        .cf-ghost{display:inline-block;padding:10px 18px;border:1px solid #e0e0e0;border-radius:8px;background:#fff;color:#16223b;font-size:14px;text-decoration:none}
        .cf-ghost:hover{border-color:#243f86}
        .cf-toolbar{display:flex;justify-content:space-between;align-items:center;margin-bottom:18px}

        /* ---- the certificate itself ---- */
        .cert{width:100%;max-width:960px;aspect-ratio:1.414/1;margin:0 auto;padding:16px;background:#fff;box-shadow:0 10px 30px rgba(16,24,40,.12);box-sizing:border-box}
        .cert-frame{display:flex;flex-direction:column;align-items:center;justify-content:space-between;height:100%;padding:26px 50px;border:4px double #c9a24d;box-sizing:border-box;text-align:center}
        .cert-brand{font-family:'Merriweather',Georgia,serif;font-size:22px;font-weight:700;letter-spacing:2px;color:#243f86}
        .cert-kicker{margin-top:6px;font-size:13px;letter-spacing:4px;text-transform:uppercase;color:#8a6d2b}
        .cert-line{font-size:14px;color:#6b7385}
        .cert-name{margin:8px 0;padding:0 30px 6px;border-bottom:1px solid #d9d4c8;font-family:'Merriweather',Georgia,serif;font-size:38px;font-weight:700;color:#111c3d}
        .cert-course{margin-top:6px;font-family:'Merriweather',Georgia,serif;font-size:25px;font-weight:700;line-height:1.3;color:#243f86}
        .cert-sub{margin-top:4px;font-size:14px;color:#6b7385}
        .cert-foot{display:grid;grid-template-columns:1fr auto 1fr;align-items:end;gap:24px;width:100%}
        .cert-val{padding-bottom:6px;border-bottom:1px solid #9aa0ad;font-size:15px;font-weight:600;color:#111c3d}
        .cert-lbl{margin-top:6px;font-size:11px;letter-spacing:1px;text-transform:uppercase;color:#7b8697}
        .cert-seal{display:flex;align-items:center;justify-content:center;width:80px;height:80px;border-radius:50%;background:#c9a24d;color:#fff;font-size:32px;box-shadow:0 0 0 4px #fff,0 0 0 6px #c9a24d}
        .cert-id{font-size:11px;letter-spacing:1px;color:#8a8f9c}

        @media print{
            @page{size:A4 landscape;margin:10mm}
            html,body{background:#fff !important}
            .sidebar,.topbar,.cf-toolbar{display:none !important}
            .main{margin:0 !important;padding:0 !important}
            .cert{width:277mm !important;max-width:none !important;height:190mm !important;aspect-ratio:auto !important;margin:0 !important;box-shadow:none !important;page-break-inside:avoid}
            *{-webkit-print-color-adjust:exact;print-color-adjust:exact}
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <uc:Sidebar ID="Sidebar1" runat="server" />

            <main class="main">
                <div class="topbar">
                    <div>
                        <div class="title"><asp:Literal ID="litHeading" runat="server" Mode="Encode"></asp:Literal></div>
                        <div class="subtitle"><asp:Literal ID="litSub" runat="server" Mode="Encode"></asp:Literal></div>
                    </div>
                </div>

                <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="msg error">
                    <asp:Literal ID="litError" runat="server" Mode="Encode"></asp:Literal>
                </asp:Panel>

                <!-- list of certificates -->
                <asp:Panel ID="pnlList" runat="server">
                    <p class="cf-summary"><asp:Literal ID="litSummary" runat="server" Mode="Encode"></asp:Literal></p>

                    <asp:Panel ID="pnlNone" runat="server" Visible="false" CssClass="cf-empty">
                        You are not enrolled in any course yet, so there are no certificates to earn.
                        <br /><a href="myCourses.aspx">Browse courses</a>
                    </asp:Panel>

                    <asp:Panel ID="pnlReady" runat="server" Visible="false">
                        <h3 class="cf-h">Ready to Claim</h3>
                        <div class="cf-grid">
                            <asp:Repeater ID="rptReady" runat="server">
                                <ItemTemplate>
                                    <div class="cf-card ready">
                                        <span class="cf-badge">Ready to claim</span>
                                        <div class="cf-title"><%#: Eval("Title") %></div>
                                        <div class="cf-meta"><%#: Eval("LevelCategory") %></div>
                                        <div class="cf-meta">Tutor: <%#: Eval("Tutor") %></div>
                                        <div class="cf-meta">All lessons completed and quizzes passed.</div>
                                        <button type="submit" name="claim" value='<%# Eval("CourseId") %>' class="cf-solid full">Claim Certificate</button>
                                    </div>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                    </asp:Panel>

                    <asp:Panel ID="pnlEarned" runat="server" Visible="false">
                        <h3 class="cf-h">Earned Certificates</h3>
                        <div class="cf-grid">
                            <asp:Repeater ID="rptEarned" runat="server">
                                <ItemTemplate>
                                    <div class="cf-card">
                                        <span class="cf-badge">Earned</span>
                                        <div class="cf-title"><%#: Eval("Title") %></div>
                                        <div class="cf-meta"><%#: Eval("LevelCategory") %></div>
                                        <div class="cf-meta">Issued <%#: Eval("DateText") %></div>
                                        <div class="cf-code">ID: <%#: Eval("Code") %></div>
                                        <a class="cf-solid full" href='<%# Eval("ViewUrl") %>'>View Certificate</a>
                                    </div>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                    </asp:Panel>

                    <asp:Panel ID="pnlProgress" runat="server" Visible="false">
                        <h3 class="cf-h">In Progress</h3>
                        <div class="cf-grid">
                            <asp:Repeater ID="rptProgress" runat="server">
                                <ItemTemplate>
                                    <div class="cf-card prog">
                                        <span class="cf-badge">In progress</span>
                                        <div class="cf-title"><%#: Eval("Title") %></div>
                                        <div class="cf-meta"><%#: Eval("LevelCategory") %></div>
                                        <div class="cf-bar"><span style='width:<%# Eval("Percent") %>%'></span></div>
                                        <div class="cf-meta"><%#: Eval("LessonsText") %> &middot; <%#: Eval("QuizzesText") %></div>
                                        <div class="cf-meta">Finish every lesson and pass every quiz to unlock this certificate.</div>
                                    </div>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                    </asp:Panel>
                </asp:Panel>

                <!-- one certificate -->
                <asp:Panel ID="pnlCert" runat="server" Visible="false">
                    <div class="cf-toolbar">
                        <a class="cf-ghost" href="certificates.aspx">&larr; All certificates</a>
                        <button type="button" class="cf-solid" onclick="window.print()">Print / Save as PDF</button>
                    </div>
                    <asp:Literal ID="litCert" runat="server" Mode="PassThrough"></asp:Literal>
                </asp:Panel>
            </main>
        </div>
    </form>
</body>
</html>