<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="studentDashboard.aspx.cs" Inherits="WAPP_EduCore.pages.Student.WebForm2" %>
<%@ Register Src="~/pages/Shared/Sidebar.ascx" TagName="Sidebar" TagPrefix="uc" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Student Dashboard</title>
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/student.css" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <uc:Sidebar ID="Sidebar1" runat="server" />
            <main class="main">
                <div class="topbar">
                    <div class="greeting">Good morning, Felicia!</div>
                    <div class="search">
                        <input placeholder="Search courses, lessons, tutors..." />
                        <div style="width:36px;height:36px;border-radius:50%;background:#fff8;display:flex;align-items:center;justify-content:center">🔔</div>
                    </div>
                </div>

                <div class="hero">
                    <div style="flex:1">
                        <h2>Learn your way, on your schedule.</h2>
                        <p>You are making great progress this week. Your top milestone is 82% complete. Continue where you left off on Data Structures.</p>
                        <button class="btn">Continue Learning</button>
                    </div>
                    <div class="thought">
                        <strong>TODAY'S THOUGHT</strong>
                        <p style="margin-top:8px;font-style:italic;color:#2b2b2b">"The beautiful thing about learning is that no one can take it away from you."</p>
                        <small>— B.B. King</small>
                    </div>
                </div>

                <div class="content">
                    <div class="left-col">
                        <div>
                            <div class="section-title">
                                <div>My Active Courses</div>
                                <a href="#" style="font-size:13px;color:#3561b3">View all courses</a>
                            </div>
                            <div class="courses">
                                <div class="course-card">
                                    <div class="thumb"></div>
                                    <div class="meta">
                                        <h4>Introduction to Computer Science</h4>
                                        <small>Tutor: Dr. Robert Miller</small>
                                        <div class="progress"><span style="width:65%"></span></div>
                                    </div>
                                </div>
                                <div class="course-card">
                                    <div class="thumb" style="background:linear-gradient(135deg,#eef3fb,#e6f0ff)"></div>
                                    <div class="meta">
                                        <h4>Advanced Data Structures</h4>
                                        <small>Tutor: Prof. Elena Vance</small>
                                        <div class="progress"><span style="width:42%;background:#3561b3"></span></div>
                                    </div>
                                </div>
                                <div class="course-card">
                                    <div class="thumb" style="background:linear-gradient(135deg,#e9e2d8,#f6efe6)"></div>
                                    <div class="meta">
                                        <h4>Aesthetic Design Principles</h4>
                                        <small>Tutor: Maria Henderson</small>
                                        <div class="progress"><span style="width:88%"></span></div>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <div class="stats">
                            <div class="stat">
                                <small>Hours This Week</small>
                                <div class="num">12.5 hrs</div>
                                <small style="color:#9aa6b8">+2.1 hrs vs last week</small>
                            </div>
                            <div class="stat">
                                <small>Current Streak</small>
                                <div class="num">7 Days</div>
                                <small style="color:#9aa6b8">Personal best is 14 days</small>
                            </div>
                            <div class="stat">
                                <small>Active Courses</small>
                                <div class="num">3 Courses</div>
                                <small style="color:#9aa6b8">No pending feedback</small>
                            </div>
                        </div>
                    </div>

                    <div class="right-col">
                        <div class="card">
                            <h4 style="margin-bottom:8px">Upcoming Deadlines</h4>
                            <div class="deadline">
                                <div class="deadline-item">
                                    <div>
                                        <strong>Binary Search Trees Lab</strong>
                                        <div style="color:#7b8697;font-size:13px">Advanced Data Structures</div>
                                    </div>
                                    <div class="badge">Oct 24</div>
                                </div>
                                <div class="deadline-item">
                                    <div>
                                        <strong>Midterm Paper Proposal</strong>
                                        <div style="color:#7b8697;font-size:13px">Aesthetic Design Principles</div>
                                    </div>
                                    <div class="badge">Oct 28</div>
                                </div>
                                <div class="deadline-item">
                                    <div>
                                        <strong>Algorithm Analysis HW</strong>
                                        <div style="color:#7b8697;font-size:13px">Intro to Computer Science</div>
                                    </div>
                                    <div class="badge">Nov 02</div>
                                </div>
                            </div>
                        </div>

                        <div class="card" style="margin-top:16px">
                            <h4 style="margin-bottom:8px">Recent Achievements</h4>
                            <ul class="achievements" style="list-style:none">
                                <li>
                                    <div style="width:36px;height:36px;border-radius:8px;background:#fef1e4"></div>
                                    <div>
                                        <strong>Code Wizard</strong>
                                        <div style="color:#7b8697;font-size:13px">Unlocked recently</div>
                                    </div>
                                </li>
                                <li>
                                    <div style="width:36px;height:36px;border-radius:8px;background:#eef6ff"></div>
                                    <div>
                                        <strong>7-Day Streak Master</strong>
                                        <div style="color:#7b8697;font-size:13px">Unlocked recently</div>
                                    </div>
                                </li>
                                <li>
                                    <div style="width:36px;height:36px;border-radius:8px;background:#eefaf1"></div>
                                    <div>
                                        <strong>Flawless Lab Submit</strong>
                                        <div style="color:#7b8697;font-size:13px">Unlocked recently</div>
                                    </div>
                                </li>
                            </ul>
                        </div>
                    </div>
                </div>
            </main>
        </div>
    </form>
</body>
</html>
