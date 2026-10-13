<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="navDashboard.ascx.cs" Inherits="WAPP_EduCore.navDashboard" %>

<link rel="stylesheet" href="<%= ResolveUrl("~/Content/navDashboard.css") %>" />
<aside class="nav-dashboard" aria-label="Main navigation">
    <a class="nav-logo" href="<%= ResolveUrl("~/default.aspx") %>">
        <img src="<%= ResolveUrl("~/images/logo.png") %>" alt="EduCore" />
    </a>

    <ul class="nav-menu">
        <% if (!IsLoggedIn) { %>

        <li>
            <a href="<%= ResolveUrl("~/default.aspx") %>" class="<%= Css("home") %>">
                <svg viewBox="0 0 24 24"><path d="M3 11l9-8 9 8"/><path d="M5 10v10h14V10"/></svg>
                Home
            </a>
        </li>
        <li>
            <a href="<%= ResolveUrl("~/coursesDirectory.aspx") %>" class="<%= Css("courses") %>">
                <svg viewBox="0 0 24 24"><path d="M2 5h7a3 3 0 013 3v12a2 2 0 00-2-2H2z"/><path d="M22 5h-7a3 3 0 00-3 3v12a2 2 0 012-2h8z"/></svg>
                Explore Courses
            </a>
        </li>

        <% } else { %>

        <li>
            <a href="<%= ResolveUrl("~/dashboard.aspx") %>" class="<%= Css("dashboard") %>">
                <svg viewBox="0 0 24 24"><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/></svg>
                Dashboard
            </a>
        </li>
        <li>
            <a href="<%= ResolveUrl("~/coursesDirectory.aspx") %>" class="<%= Css("courses") %>">
                <svg viewBox="0 0 24 24"><path d="M2 5h7a3 3 0 013 3v12a2 2 0 00-2-2H2z"/><path d="M22 5h-7a3 3 0 00-3 3v12a2 2 0 012-2h8z"/></svg>
                Courses
            </a>
        </li>
        <li>
            <a href="<%= ResolveUrl("~/calendar.aspx") %>" class="<%= Css("calendar") %>">
                <svg viewBox="0 0 24 24"><rect x="3" y="4" width="18" height="17" rx="2"/><path d="M3 10h18M8 2v4M16 2v4"/></svg>
                Calendar
            </a>
        </li>
        <li>
            <a href="<%= ResolveUrl("~/grades.aspx") %>" class="<%= Css("grades") %>">
                <svg viewBox="0 0 24 24"><circle cx="12" cy="9" r="6"/><path d="M8.5 14L7 22l5-3 5 3-1.5-8"/></svg>
                Grades
            </a>
        </li>
        <li>
            <a href="<%= ResolveUrl("~/settings.aspx") %>" class="<%= Css("settings") %>">
                <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.7 1.7 0 00.3 1.8l.1.1a2 2 0 11-2.8 2.8l-.1-.1a1.7 1.7 0 00-1.8-.3 1.7 1.7 0 00-1 1.5V21a2 2 0 11-4 0v-.1a1.7 1.7 0 00-1.1-1.5 1.7 1.7 0 00-1.8.3l-.1.1a2 2 0 11-2.8-2.8l.1-.1a1.7 1.7 0 00.3-1.8 1.7 1.7 0 00-1.5-1H3a2 2 0 110-4h.1a1.7 1.7 0 001.5-1.1 1.7 1.7 0 00-.3-1.8l-.1-.1a2 2 0 112.8-2.8l.1.1a1.7 1.7 0 001.8.3H9a1.7 1.7 0 001-1.5V3a2 2 0 114 0v.1a1.7 1.7 0 001 1.5 1.7 1.7 0 001.8-.3l.1-.1a2 2 0 112.8 2.8l-.1.1a1.7 1.7 0 00-.3 1.8V9a1.7 1.7 0 001.5 1H21a2 2 0 110 4h-.1a1.7 1.7 0 00-1.5 1z"/></svg>
                Settings
            </a>
        </li>

        <% } %>
    </ul>

    <div class="nav-spacer"></div>

    <div class="nav-account">
        <% if (!IsLoggedIn) { %>
            <a class="btn-login" href="<%= LoginUrl %>">Log in</a>
            <p class="nav-hint">Log in to enroll in courses and track your progress.</p>
        <% } else { %>
            <div class="nav-user">
                <div class="nav-avatar"><%= Initial %></div>
                <div>
                    <div class="nav-user-name"><%= Server.HtmlEncode(DisplayName) %></div>
                    <div class="nav-user-role"><%= Server.HtmlEncode(Role) %></div>
                </div>
            </div>
        <% } %>
    </div>
</aside>
