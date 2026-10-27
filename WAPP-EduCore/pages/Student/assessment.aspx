<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="assessment.aspx.cs" Inherits="WAPP_EduCore.pages.Student.WebForm3" %>
<%@ Register Src="~/pages/Shared/Sidebar.ascx" TagName="Sidebar" TagPrefix="uc" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Assessment</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Merriweather:wght@400;700&display=swap" rel="stylesheet" />
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/student.css" rel="stylesheet" type="text/css" />
    <link href="/Content/student-upgrade.css" rel="stylesheet" />
    <script src="/Content/fx.js"></script>
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <uc:Sidebar ID="Sidebar1" runat="server" />
            <main class="main">
                <div class="topbar">
                    <div class="quiz-heading">
                        <span class="title"><asp:Literal ID="litQuizTitle" runat="server" Mode="Encode"></asp:Literal></span>
                        <span class="q-chip" id="chipBox" runat="server">Question <asp:Literal ID="litNum" runat="server"></asp:Literal> of <asp:Literal ID="litTotal" runat="server"></asp:Literal></span>
                    </div>
                </div>

                <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="result-box">
                    <h3>This quiz has no questions yet.</h3>
                    <p style="margin-top:12px"><a href="myCourses.aspx" class="btn-solid" style="text-decoration:none;display:inline-block">Back to courses</a></p>
                </asp:Panel>

                <asp:Panel ID="pnlQuiz" runat="server">
                    <div class="quiz-layout">
                        <div class="quiz-main">
                            <div class="q-card">
                                <div class="q-label">QUESTION <asp:Literal ID="litQNum" runat="server"></asp:Literal></div>
                                <h3 class="q-text"><asp:Literal ID="litQuestion" runat="server" Mode="Encode"></asp:Literal></h3>

                                <asp:Repeater ID="rptOptions" runat="server" OnItemCommand="rptOptions_ItemCommand">
                                    <ItemTemplate>
                                        <asp:LinkButton runat="server" CommandName="pick"
                                            CommandArgument='<%# Eval("Letter") %>'
                                            CssClass='<%# Eval("Css") %>'
                                            Enabled='<%# !IsLocked %>'>
                                            <span class="opt-letter"><%# Eval("Letter") %></span>
                                            <span class="opt-text"><%#: Eval("Text") %></span>
                                        </asp:LinkButton>
                                    </ItemTemplate>
                                </asp:Repeater>

                                <asp:Label ID="lblMsg" runat="server" Visible="false"
                                    Style="display:block;margin-top:10px;color:#d64545;font-size:13px"></asp:Label>

                                <div class="q-actions">
                                    <div>
                                        <asp:Button ID="btnPrev" runat="server" Text="Previous Question"
                                            CssClass="btn-ghost" OnClick="btnPrev_Click" />
                                    </div>
                                    <div>
                                        <asp:Button ID="btnCheck" runat="server" Text="Check Answer"
                                            CssClass="btn-solid" OnClick="btnCheck_Click" />
                                        <asp:Button ID="btnNext" runat="server" Text="Next Question"
                                            CssClass="btn-solid" OnClick="btnNext_Click" />
                                    </div>
                                </div>
                            </div>

                            <asp:Panel ID="pnlExplain" runat="server" Visible="false" CssClass="explain ok">
                                <strong><asp:Literal ID="litExplainHead" runat="server" Mode="Encode"></asp:Literal></strong>
                                <asp:Literal ID="litExplain" runat="server" Mode="Encode"></asp:Literal>
                            </asp:Panel>
                        </div>

                        <div class="quiz-side">
                            <h3>Assessment Progress</h3>
                            <div class="dots">
                                <asp:Repeater ID="rptNav" runat="server" OnItemCommand="rptNav_ItemCommand">
                                    <ItemTemplate>
                                        <asp:LinkButton runat="server" CommandName="go"
                                            CommandArgument='<%# Eval("Index") %>'
                                            CssClass='<%# Eval("Css") %>'><%# Eval("Number") %></asp:LinkButton>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </div>
                           <asp:Button ID="btnSubmit" runat="server" Text="Submit Quiz"
                                CssClass="btn-start" OnClientClick="openSubmitModal(); return false;" />
                        </div>
                    </div>
                </asp:Panel>

                <div id="submitModal" class="modal-back">
            <div class="modal-box">
                <div class="modal-head">
                    <h3>Submit Assessment</h3>
                </div>
                <p class="modal-text">Are you sure you want to submit your quiz? Unanswered questions will count as wrong.</p>
                <div class="modal-buttons">
                    <button type="button" class="cl-btn-ghost" onclick="closeSubmitModal()">Cancel</button>
                    <asp:Button ID="btnConfirmSubmit" runat="server" Text="Yes, Submit" CssClass="cl-btn-solid" OnClick="btnSubmit_Click" />
                </div>
            </div>
        </div>

                <asp:Panel ID="pnlResult" runat="server" Visible="false" CssClass="result-box">
                    <div class="access-label">Your Result</div>
                    <div class="result-score"><asp:Literal ID="litScore" runat="server"></asp:Literal>%</div>
                    <h3 style="margin:10px 0 6px"><asp:Literal ID="litResultText" runat="server"></asp:Literal></h3>
                    <p style="color:#6b7385;font-size:14px">Grade: <strong><asp:Literal ID="litGrade" runat="server" Mode="Encode"></asp:Literal></strong></p>
    
                    <div style="margin-top:26px;display:flex;gap:12px;justify-content:center">
                        <asp:HyperLink ID="lnkRetake" runat="server" CssClass="btn-ghost" Style="text-decoration:none">Retake Quiz</asp:HyperLink>
                        <asp:HyperLink ID="lnkBack" runat="server" CssClass="btn-solid" Style="text-decoration:none">Back to Course</asp:HyperLink>
                    </div>

                    <%-- Small row for guests below the buttons --%>
                    <asp:Panel ID="pnlGuestLoginPrompt" runat="server" Visible="false" style="margin-top: 18px; font-size: 13px; color: #6b7385;">
                        Log in to save your score. <a href="../../Auth/login.aspx" style="color: #243f86; font-weight: 600; text-decoration: underline;">Login</a>
                    </asp:Panel>
                </asp:Panel>
            </main>
        </div>
    </form>

<script type="text/javascript">
function openSubmitModal() {
    var modal = document.getElementById('submitModal');
    if (modal) modal.classList.add('open');
}
function closeSubmitModal() {
    var modal = document.getElementById('submitModal');
    if (modal) modal.classList.remove('open');
}
</script>
</body>
</html>