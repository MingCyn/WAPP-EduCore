<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="tutorNav.aspx.cs" Inherits="WAPP_EduCore.pages.Tutor.tutorNav" %>

<header class="navbar">
    <div class="nav-brand">
        <img src="~/images/EduCore_Logo.png" alt="EduCore Logo" class="nav-logo" runat="server" />
    </div>

    <nav class="nav-links">
        <!-- Dashboard -->
        <a href="tutorDashboard.aspx" class="nav-link">
            <svg class="menu-icon ic-dash" viewBox="0 0 24 24" aria-hidden="true">
                <rect x="3" y="3" width="7" height="7" rx="1.6" />
                <rect x="14" y="3" width="7" height="7" rx="1.6" />
                <rect x="14" y="14" width="7" height="7" rx="1.6" />
                <rect x="3" y="14" width="7" height="7" rx="1.6" />
            </svg><span>Dashboard</span></a>

        <!-- My Courses -->
        <a href="courses.aspx" class="nav-link">
            <svg class="menu-icon ic-book" viewBox="0 0 24 24" aria-hidden="true">
                <path class="ic-book-l" d="M2 4h6a4 4 0 0 1 4 4v13a3 3 0 0 0-3-3H2z" />
                <path class="ic-book-r" d="M22 4h-6a4 4 0 0 0-4 4v13a3 3 0 0 1 3-3h7z" />
            </svg><span>My Courses</span></a>

        <!-- Students -->
        <a href="studentManagement.aspx" class="nav-link">
            <svg class="menu-icon ic-hop" viewBox="0 0 24 24" aria-hidden="true">
                <path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" />
                <circle cx="9" cy="7" r="4" />
                <path d="M23 21v-2a4 4 0 00-3-3.87" />
                <path d="M16 3.13a4 4 0 010 7.75" />
            </svg><span>Students</span></a>

        <!-- Messages -->
        <a href="messages.aspx" class="nav-link">
            <svg class="menu-icon ic-mail" viewBox="0 0 24 24" aria-hidden="true">
                <g class="ic-mail-body">
                    <rect x="2" y="4" width="20" height="16" rx="2" />
                    <path class="ic-mail-flap" d="m22 7-8.97 5.7a1.94 1.94 0 0 1-2.06 0L2 7" />
                </g>
            </svg><span>Messages</span></a>

        <!-- Account -->
        <a href="settings.aspx" class="nav-link">
            <svg class="menu-icon ic-gear" viewBox="0 0 24 24" aria-hidden="true">
                <g class="ic-gear-body">
                    <path d="M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z" />
                    <circle cx="12" cy="12" r="3" />
                </g>
            </svg><span>Account</span></a>
    </nav>

    <div class="nav-profile">
        <span class="nav-avatar" id="navAvatar">?</span>
        <span class="profile-name"><%: DisplayName %></span>
        <a href="logout.aspx" class="nav-logout">Log out</a>
    </div>
</header>

<script>
    (function () {
        // ---- active link ----
        var groups = {
            'tutordashboard.aspx': ['tutordashboard.aspx'],
            'courses.aspx': ['courses.aspx', 'coursebuilder.aspx', 'coursedetails.aspx', 'courseanalysis.aspx', 'assessmentbuilder.aspx'],
            'studentmanagement.aspx': ['studentmanagement.aspx', 'studentperformance.aspx'],
            'messages.aspx': ['messages.aspx'],
            'settings.aspx': ['settings.aspx']
        };
        var page = location.pathname.split('/').pop().toLowerCase();
        var target = null;
        Object.keys(groups).forEach(function (href) {
            if (groups[href].indexOf(page) !== -1) target = href;
        });
        document.querySelectorAll('.nav-link').forEach(function (a) {
            var h = (a.getAttribute('href') || '').toLowerCase();
            a.classList.toggle('active', h === target);
        });

        // ---- initials avatar (first + last name letters, like the student sidebar) ----
        var nameEl = document.querySelector('.profile-name');
        var av = document.getElementById('navAvatar');
        if (nameEl && av) {
            var parts = (nameEl.textContent || '').trim().split(/\s+/).filter(Boolean);
            if (parts.length) {
                var ini = parts[0].charAt(0);
                if (parts.length > 1) ini += parts[parts.length - 1].charAt(0);
                av.textContent = ini.toUpperCase();
            }
        }
    })();
</script>
