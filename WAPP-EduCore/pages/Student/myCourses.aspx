<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="myCourses.aspx.cs" Inherits="WAPP_EduCore.pages.Student.WebForm1" %>
<%@ Register Src="~/pages/Shared/Sidebar.ascx" TagName="Sidebar" TagPrefix="uc" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Explore Courses</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Merriweather:wght@400;700&display=swap" rel="stylesheet" />
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/student.css" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <uc:Sidebar ID="Sidebar1" runat="server" />
            <main class="main">
                <div class="topbar">
                    <div class="title">Explore Courses</div>
                    <div class="search">
                        <asp:TextBox ID="txtSearch" runat="server" AutoPostBack="true"
                            OnTextChanged="Filters_Changed"
                            placeholder="Search courses by title or topic..."></asp:TextBox>
                    </div>
                </div>

                <div class="filters">
                    <div class="filter-row">
                        <asp:LinkButton ID="btnAllLevels" runat="server" OnClick="btnAllLevels_Click"
                            CssClass="pill">All</asp:LinkButton>
                        <asp:Repeater ID="rptLevels" runat="server" OnItemCommand="rptLevels_ItemCommand">
                            <ItemTemplate>
                                <asp:LinkButton runat="server" CommandName="level"
                                    CommandArgument='<%# Eval("course_level") %>'
                                    CssClass='<%# PillClass(Eval("course_level")) %>'><%#: Eval("course_level") %></asp:LinkButton>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                    <div class="filter-row">
                        <asp:DropDownList ID="ddlCategory" runat="server" AutoPostBack="true"
                            OnSelectedIndexChanged="Filters_Changed"></asp:DropDownList>
                        <asp:DropDownList ID="ddlSort" runat="server" AutoPostBack="true"
                            OnSelectedIndexChanged="Filters_Changed">
                            <asp:ListItem Value="popular" Text="Sort by: Most Popular" />
                            <asp:ListItem Value="newest" Text="Sort by: Newest" />
                            <asp:ListItem Value="title" Text="Sort by: Title (A-Z)" />
                        </asp:DropDownList>
                    </div>
                </div>

                <asp:Label ID="lblMessage" runat="server" Visible="false"
                    Style="display:block;margin:10px 0;color:#2563eb"></asp:Label>

                <asp:Panel ID="pnlEmpty" runat="server" Visible="false"
                    Style="padding:40px;text-align:center;color:#7b8697">
                    No courses match your filters.
                </asp:Panel>

                <div class="grid">
                    <asp:Repeater ID="rptCourses" runat="server" OnItemCommand="rptCourses_ItemCommand">
                        <ItemTemplate>
                            <div class="course">
                                <div class="media" style="<%# MediaStyle(Eval("course_img")) %>"></div>
                                <div class="body">
                                    <div class="meta">
                                        <span class="tag"><%#: Eval("course_level") %></span>
                                        <span class="duration"><%#: Eval("duration") %></span>
                                    </div>
                                    <h4><%#: Eval("title") %></h4>
                                    <small><%#: Eval("instructor") %></small>
                                    <asp:PlaceHolder runat="server" Visible='<%# Convert.ToBoolean(Eval("is_enrolled")) %>'>
                                        <div class="progress"><div class="progress-bar" style='width:<%# Percent(Eval("total_lessons"), Eval("done_lessons")) %>%'></div></div>
                                        <small class="progress-text"><%# Percent(Eval("total_lessons"), Eval("done_lessons")) %>% complete</small>
                                    </asp:PlaceHolder>
                                </div>
                                <div class="footer">
                                    <span class="students"><%#: Eval("student_count") %> students</span>
                                    <asp:Button runat="server" CssClass="btn"
                                        CommandName='<%# Convert.ToBoolean(Eval("is_enrolled")) ? "view" : "enroll" %>'
                                        CommandArgument='<%# Eval("course_id") %>'
                                        Text='<%# Convert.ToBoolean(Eval("is_enrolled")) ? "View Course" : "Enroll" %>' />
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
            </main>
        </div>
    </form>
</body>
</html>