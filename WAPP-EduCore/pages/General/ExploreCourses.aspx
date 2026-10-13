<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ExploreCourses.aspx.cs" Inherits="WAPP_EduCore.ExploreCourses" %>

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Explore Courses - EduCore</title>
    <link href="https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700;800&display=swap" rel="stylesheet">
    <link rel="stylesheet" href="landing.css">
    <style>
        /* Dashboard layout styling matching your design */
        body { margin: 0; font-family: 'Plus Jakarta Sans', sans-serif; background-color: #f8fafc; display: flex; }
        .dashboard-container { display: flex; width: 100vw; min-height: 100vh; }
        
        /* Left Sidebar */
        .sidebar { width: 260px; background-color: #0c1b33; color: #fff; display: flex; flex-direction: column; justify-content: space-between; padding: 24px; }
        .sidebar .brand img { width: 120px; margin-bottom: 30px; }
        .sidebar-links { display: flex; flex-direction: column; gap: 8px; }
        .sidebar-links a { color: #94a3b8; text-decoration: none; padding: 12px 16px; border-radius: 8px; font-weight: 500; transition: 0.2s; }
        .sidebar-links a:hover, .sidebar-links a.active { background-color: #1e3a8a; color: #fff; }
        .sidebar-user { display: flex; align-items: center; gap: 12px; border-top: 1px solid #1e293b; padding-top: 16px; }
        .sidebar-user img { width: 40px; height: 40px; border-radius: 50%; object-fit: cover; }
        .sidebar-user .user-info h5 { margin: 0; font-size: 14px; color: #fff; }
        .sidebar-user .user-info p { margin: 0; font-size: 12px; color: #94a3b8; }

        /* Main Content Area */
        .main-content { flex: 1; padding: 32px; overflow-y: auto; }
        .top-bar { display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px; }
        .top-bar h2 { margin: 0; font-size: 24px; color: #0f172a; }
        .search-bar { position: relative; width: 300px; }
        .search-bar input { width: 100%; padding: 10px 16px 10px 40px; border-radius: 20px; border: 1px solid #cbd5e1; outline: none; font-size: 14px; }
        
        /* Filter Pills */
        .filter-pills { display: flex; gap: 8px; margin-bottom: 20px; flex-wrap: wrap; }
        .pill { padding: 8px 16px; border-radius: 20px; border: 1px solid #cbd5e1; background: #fff; color: #475569; font-size: 14px; cursor: pointer; text-decoration: none; }
        .pill.active, .pill:hover { background-color: #1e3a8a; color: #fff; border-color: #1e3a8a; }

        /* Courses Grid */
        .courses-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(320px, 1fr)); gap: 24px; }
        .course-card-dash { background: #fff; border-radius: 12px; border: 1px solid #e2e8f0; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.02); display: flex; flex-direction: column; justify-content: space-between; }
        .card-img { height: 160px; background-size: cover; background-position: center; }
        .card-content { padding: 16px; }
        .card-meta-top { display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px; }
        .tag-badge { background: #eff6ff; color: #1d4ed8; padding: 4px 10px; border-radius: 6px; font-size: 12px; font-weight: 600; }
        .rating-star { color: #d97706; font-size: 13px; font-weight: 600; }
        .card-content h4 { margin: 0 0 6px 0; font-size: 16px; color: #1e293b; }
        .card-content p { margin: 0; font-size: 13px; color: #64748b; }
        .card-footer { padding: 12px 16px; border-top: 1px solid #f1f5f9; display: flex; justify-content: space-between; align-items: center; }
        .student-count { font-size: 12px; color: #94a3b8; }
        .btn-action { background: #1e3a8a; color: #fff; padding: 6px 14px; border-radius: 6px; font-size: 13px; text-decoration: none; font-weight: 500; transition: 0.2s; }
        .btn-action:hover { background: #1d4ed8; }
    </style>
</head>
<body>
    <form id="form1" runat="server" class="dashboard-container">
        
        <!-- SIDEBAR -->
        <div class="sidebar">
            <div>
                <div class="brand">
                    <img src="images/logo.png" alt="EduCore">
                </div>
                <div class="sidebar-links">
                    <a href="#">Dashboard</a>
                    <a href="ExploreCourses.aspx" class="active">My Courses</a>
                    <a href="#">Calendar</a>
                    <a href="#">Messages</a>
                    <a href="#">Grades</a>
                    <a href="#">Certificates</a>
                    <a href="#">Settings</a>
                </div>
            </div>
            <div class="sidebar-user">
                <img src="images/study.jpg" alt="User">
                <div class="user-info">
                    <h5>Sin Yi Lai</h5>
                    <p>Student ID: 48922</p>
                </div>
            </div>
        </div>

        <!-- MAIN SECTION -->
        <div class="main-content">
            <div class="top-bar">
                <h2>Explore Courses</h2>
                <div class="search-bar">
                    <asp:TextBox ID="txtSearch" runat="server" placeholder="Search courses, lessons, resources..." AutoPostBack="true" OnTextChanged="txtSearch_TextChanged"></asp:TextBox>
                </div>
            </div>

            <!-- FILTER PILLS -->
            <div class="filter-pills">
                <a href="ExploreCourses.aspx" class="pill active">All Courses</a>
                <a href="ExploreCourses.aspx?cat=Computer Science" class="pill">Computer Science</a>
                <a href="ExploreCourses.aspx?cat=Design" class="pill">Design</a>
                <a href="ExploreCourses.aspx?cat=Advanced Tech" class="pill">Advanced Tech</a>
            </div>

            <!-- REPEATER GRID -->
            <div class="courses-grid">
                <asp:Repeater ID="RepeaterAllCourses" runat="server">
                    <ItemTemplate>
                        <div class="course-card-dash">
                            <div class="card-img" style="background-image: url('<%# Eval("course_img") %>');"></div>
                            <div class="card-content">
                                <div class="card-meta-top">
                                    <span class="tag-badge"><%# Eval("category") %></span>
                                    <span class="rating-star">★ 4.9</span>
                                </div>
                                <h4><%# Eval("title") %></h4>
                                <p><%# Eval("tutor_name") %></p>
                            </div>
                            <div class="card-footer">
                                <span class="student-count">1,240 students</span>
                                <!-- Clicking View Course passes the course_id to a detail page to view lessons -->
                                <a href='<%# "CourseDetails.aspx?course_id=" + Eval("course_id") %>' class="btn-action">View Course</a>
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </div>

    </form>
</body>
</html>