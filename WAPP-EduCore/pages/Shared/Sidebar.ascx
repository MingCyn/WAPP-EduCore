<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Sidebar.ascx.cs" Inherits="WAPP_EduCore.pages.Shared.Sidebar" %>
<div class="sidebar">
    <img src="../../images/logo.png" alt="EduCore" class="logo" />

    <a id="lnkDashboard" href="~/pages/Student/studentDashboard.aspx" class="menu-item" runat="server"><img src="../../images/dashboard_icon.png" alt="" class="menu-icon" /><span>Dashboard</span></a>
    <a href="~/pages/Student/myCourses.aspx" class="menu-item" runat="server"><img src="../../images/mycourse_icon.png" alt="" class="menu-icon" /><span>My Courses</span></a>
    <a href="~/pages/Student/calendar.aspx" class="menu-item" runat="server"><img src="../../images/calendar_icon.png" alt="" class="menu-icon" /><span>Calendar</span></a>
    <a href="~/pages/Student/messages.aspx" class="menu-item" runat="server"><img src="../../images/messages_icon.png" alt="" class="menu-icon" /><span>Messages</span></a>
    <a href="~/pages/Student/result.aspx" class="menu-item" runat="server"><img src="../../images/grades_icon.png" alt="" class="menu-icon" /><span>Grades</span></a>
    <a href="~/pages/Student/certificates.aspx" class="menu-item" runat="server"><img src="../../images/certificates_icon.png" alt="" class="menu-icon" /><span>Certificates</span></a>
    <a href="~/pages/Student/settings.aspx" class="menu-item" runat="server"><img src="../../images/settings_icon.png" alt="" class="menu-icon" /><span>Settings</span></a>


<!-- LOGGED IN PANEL -->
    <asp:Panel ID="pnlLoggedIn" runat="server">
        <div class="me">
            <span class="me-avatar" id="lblInitials" runat="server">?</span>
            <div class="me-text">
                <strong id="lblName" runat="server">Student Name</strong>
                <small id="lblId" runat="server">Student ID: 00000</small>
            </div>
        </div>
        <asp:LinkButton ID="btnLogout" runat="server" CssClass="logout-btn" OnClick="btnLogout_Click" CausesValidation="false">Log out</asp:LinkButton>
    </asp:Panel>

    <asp:Panel ID="pnlGuest" runat="server" Visible="false" CssClass="sidebar-guest-box">
        <p class="guest-msg">Log in to enroll in courses and track your progress</p>
        <asp:HyperLink ID="lnkSidebarLogin" runat="server" NavigateUrl="~/Auth/login.aspx" CssClass="sidebar-login-btn">Log In</asp:HyperLink>
    </asp:Panel>
</div>