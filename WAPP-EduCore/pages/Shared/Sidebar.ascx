<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Sidebar.ascx.cs" Inherits="WAPP_EduCore.pages.Shared.Sidebar" %>
<div class="sidebar">
    <img src="../../images/logo.png" alt="EduCore" class="logo" />

    <!-- Student Dashboard -->
    <a id="lnkDashboard" href="~/pages/Student/studentDashboard.aspx" class="menu-item"
        runat="server">
        <svg class="menu-icon ic-dash" viewBox="0 0 24 24" aria-hidden="true">
            <rect x="3" y="3" width="7" height="7" rx="1.6" />
            <rect x="14" y="3" width="7" height="7" rx="1.6" />
            <rect x="14" y="14" width="7" height="7" rx="1.6" />
            <rect x="3" y="14" width="7" height="7" rx="1.6" />
        </svg><span>Dashboard</span></a>

    <!-- My Courses -->
    <a href="~/pages/Student/myCourses.aspx" class="menu-item" runat="server">
        <svg class="menu-icon ic-book" viewBox="0 0 24 24" aria-hidden="true">
            <path class="ic-book-l" d="M2 4h6a4 4 0 0 1 4 4v13a3 3 0 0 0-3-3H2z" />
            <path class="ic-book-r" d="M22 4h-6a4 4 0 0 0-4 4v13a3 3 0 0 1 3-3h7z" />
        </svg><span>My Courses</span></a>

    <!-- Virtual Lab -->
    <a href="~/pages/Student/virtualLab.aspx" class="menu-item" runat="server">
        <svg class="menu-icon ic-flask" viewBox="0 0 24 24" aria-hidden="true">
            <g class="ic-flask-body">
                <path d="M10 2v7.527a2 2 0 0 1-.211.896L4.72 20.55a1 1 0 0 0 .9 1.45h12.76a1 1 0 0 0 .9-1.45l-5.069-10.127A2 2 0 0 1 14 9.527V2" />
                <path d="M8.5 2h7" />
                <path d="M7 16h10" />
            </g>
            <circle class="ic-bub1" cx="10.5" cy="18.5" r="0.9" fill="currentColor" stroke="none" />
            <circle class="ic-bub2" cx="14" cy="19" r="0.7" fill="currentColor" stroke="none" />
        </svg><span>Virtual Lab</span></a>

    <!-- Calendar -->
    <a href="~/pages/Student/calendar.aspx" class="menu-item" runat="server">
        <svg class="menu-icon ic-cal" viewBox="0 0 24 24" aria-hidden="true">
            <rect x="3" y="4" width="18" height="18" rx="2" />
            <path class="ic-cal-tab" d="M16 2v4" />
            <path class="ic-cal-tab" d="M8 2v4" />
            <path d="M3 10h18" />
            <circle class="ic-cal-dot" cx="12" cy="16" r="1.4" fill="currentColor" stroke="none" />
        </svg><span>Calendar</span></a>

    <!-- Messages -->
    <a href="~/pages/Student/messages.aspx" class="menu-item" runat="server">
        <svg class="menu-icon ic-mail" viewBox="0 0 24 24" aria-hidden="true">
            <g class="ic-mail-body">
                <rect x="2" y="4" width="20" height="16" rx="2" />
                <path class="ic-mail-flap" d="m22 7-8.97 5.7a1.94 1.94 0 0 1-2.06 0L2 7" />
            </g>
        </svg><span>Messages</span></a>

    <!-- Grades -->
    <a href="~/pages/Student/result.aspx" class="menu-item" runat="server">
        <svg class="menu-icon ic-bars" viewBox="0 0 24 24" aria-hidden="true">
            <line x1="6" y1="20" x2="6" y2="14" stroke-width="3" />
            <line x1="12" y1="20" x2="12" y2="5" stroke-width="3" />
            <line x1="18" y1="20" x2="18" y2="10" stroke-width="3" />
        </svg><span>Grades</span></a>

    <!-- Certificates -->
    <a href="~/pages/Student/certificates.aspx" class="menu-item" runat="server">
        <svg class="menu-icon ic-award" viewBox="0 0 24 24" aria-hidden="true">
            <g class="ic-award-body">
                <circle cx="12" cy="8" r="6" />
                <path d="M15.477 12.89 17 22l-5-3-5 3 1.523-9.11" />
            </g>
        </svg><span>Certificates</span></a>

    <!-- Settings -->
    <a href="~/pages/Student/settings.aspx" class="menu-item" runat="server">
        <svg class="menu-icon ic-gear" viewBox="0 0 24 24" aria-hidden="true">
            <g class="ic-gear-body">
                <path d="M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z" />
                <circle cx="12" cy="12" r="3" />
            </g>
        </svg><span>Settings</span></a>

    <!-- LOGGED IN PANEL -->
    <asp:Panel ID="pnlLoggedIn" runat="server">
        <div class="me">
            <span class="me-avatar" id="lblInitials" runat="server">?</span>
            <div class="me-text">
                <strong id="lblName" runat="server">Student Name</strong>
                <small id="lblId" runat="server">Student ID: 00000</small>
            </div>
        </div>
        <asp:LinkButton ID="btnLogout" runat="server" CssClass="logout-btn" OnClick="btnLogout_Click"
            CausesValidation="false">Log out</asp:LinkButton>
    </asp:Panel>

    <asp:Panel ID="pnlGuest" runat="server" Visible="false" CssClass="sidebar-guest-box">
        <p class="guest-msg">Log in to enroll in courses and track your progress</p>
        <asp:HyperLink ID="lnkSidebarLogin" runat="server" NavigateUrl="~/Auth/login.aspx"
            CssClass="sidebar-login-btn">Log In</asp:HyperLink>
    </asp:Panel>

    <%@ Register Src="~/pages/Shared/ChatWidget.ascx" TagName="ChatWidget" TagPrefix="uc" %>
    <uc:chatwidget id="ChatWidget1" runat="server" />
</div>
