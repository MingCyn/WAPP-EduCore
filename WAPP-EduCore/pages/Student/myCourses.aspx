<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="myCourses.aspx.cs" Inherits="WAPP_EduCore.pages.Student.WebForm1" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Explore Courses</title>
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/student.css" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <div class="sidebar">
                <img src="../../images/logo.png" alt="EduCore" class="logo" />

                <a class="menu-item" href="studentDashboard.aspx">Dashboard</a>
                <a class="menu-item active" href="myCourses.aspx">My Courses</a>
                <a class="menu-item" href="#">Calendar</a>
                <a class="menu-item" href="#">Messages</a>
                <a class="menu-item" href="#">Grades</a>
                <a class="menu-item" href="#">Certificates</a>
                <a class="menu-item" href="#">Settings</a>

                <div class="me">
                    <img src="../../images/profile-placeholder.png" alt="Profile" style="width:36px;height:36px;border-radius:50%;vertical-align:middle;margin-right:8px;" />
                    <div style="display:inline-block;vertical-align:middle">
                        <div><strong>Priya Sharma</strong></div>
                        <small>Student ID: 48922</small>
                    </div>
                </div>
            </div>
            <main class="main">
                <div class="topbar">
                    <div class="title">Explore Courses</div>
                    <div class="search"><input placeholder="Search courses, lessons, resources..." /></div>
                </div>

                <div class="filters">
                    <div class="filter-row">
                        <button class="pill">Primary</button>
                        <button class="pill">Secondary</button>
                        <button class="pill active">Undergraduate</button>
                        <button class="pill">Professional</button>
                        <button class="pill">Lifelong</button>
                    </div>
                    <div style="flex:1"></div>
                    <div class="filter-row">
                        <select style="padding:8px 12px;border-radius:8px;border:1px solid #e8e8e8">
                            <option>Topic: Computer Science</option>
                        </select>
                        <select style="padding:8px 12px;border-radius:8px;border:1px solid #e8e8e8">
                            <option>Sort by: Most Popular</option>
                        </select>
                    </div>
                </div>

                <div class="grid">
                    <div class="course">
                        <div class="media"></div>
                        <div class="body">
                            <div class="meta"><span class="tag">Undergraduate</span><span>⭐ 4.8</span></div>
                            <h4>Introduction to Computer Science</h4>
                            <small>Dr. Robert Miller</small>
                            <div style="margin-top:10px;color:#7b8697;font-size:13px">1,240 students</div>
                        </div>
                        <div class="footer"><div></div><button class="btn">View Course</button></div>
                    </div>

                    <div class="course">
                        <div class="media" style="background:linear-gradient(135deg,#eef3fb,#e6f0ff)"></div>
                        <div class="body">
                            <div class="meta"><span class="tag">Professional</span><span>⭐ 4.9</span></div>
                            <h4>Advanced Data Structures</h4>
                            <small>Prof. Elena Vance</small>
                            <div style="margin-top:10px;color:#7b8697;font-size:13px">542 students</div>
                        </div>
                        <div class="footer"><div></div><button class="btn">View Course</button></div>
                    </div>

                    <div class="course">
                        <div class="media" style="background:linear-gradient(135deg,#e9e2d8,#f6efe6)"></div>
                        <div class="body">
                            <div class="meta"><span class="tag">Undergraduate</span><span>⭐ 4.7</span></div>
                            <h4>Aesthetic Design Principles</h4>
                            <small>Maria Henderson</small>
                            <div style="margin-top:10px;color:#7b8697;font-size:13px">892 students</div>
                        </div>
                        <div class="footer"><div></div><button class="btn">View Course</button></div>
                    </div>

                    <div class="course">
                        <div class="media" style="background:#0f324f;display:flex;align-items:center;justify-content:center;color:#fff">Diagram</div>
                        <div class="body">
                            <div class="meta"><span class="tag">Undergraduate</span><span>⭐ 4.9</span></div>
                            <h4>Applied Discrete Mathematics</h4>
                            <small>Dr. Alan Turing</small>
                            <div style="margin-top:10px;color:#7b8697;font-size:13px">310 students</div>
                        </div>
                        <div class="footer"><div></div><button class="btn">Enroll</button></div>
                    </div>

                    <div class="course">
                        <div class="media" style="background:url('/images/instructor1.jpg') center/cover"></div>
                        <div class="body">
                            <div class="meta"><span class="tag">Professional</span><span>⭐ 4.6</span></div>
                            <h4>Fundamentals of UX/UI Design</h4>
                            <small>Sarah Jenkins</small>
                            <div style="margin-top:10px;color:#7b8697;font-size:13px">1,450 students</div>
                        </div>
                        <div class="footer"><div></div><button class="btn">Enroll</button></div>
                    </div>

                    <div class="course">
                        <div class="media" style="background:url('/images/instructor2.jpg') center/cover"></div>
                        <div class="body">
                            <div class="meta"><span class="tag">Professional</span><span>⭐ 5.0</span></div>
                            <h4>Machine Learning Foundations</h4>
                            <small>Prof. Andrew Ng</small>
                            <div style="margin-top:10px;color:#7b8697;font-size:13px">2,100 students</div>
                        </div>
                        <div class="footer"><div></div><button class="btn">Enroll</button></div>
                    </div>
                </div>
            </main>
        </div>
    </form>
</body>
</html>
