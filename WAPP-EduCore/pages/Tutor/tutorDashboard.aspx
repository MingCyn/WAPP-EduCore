<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="tutorDashboard.aspx.cs" Inherits="WAPP_EduCore.pages.Tutor.tutorDashboard" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>EduCore - Tutor Dashboard</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet" />
    <link href="../../Content/dashboard.css" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
        <div class="dashboard-layout">
            
            <!-- Top Navigation Bar -->
            <header class="navbar">
                <div class="nav-brand">
                    <img src="../../images/EduCore_Logo.png" alt="EduCore Logo" class="nav-logo" />
                </div>
                <nav class="nav-links">
                    <a href="tutorDashboard.aspx" class="nav-link active">Dashboard</a>
                    <a href="courses.aspx" class="nav-link">My Courses</a>
                    <a href="students.aspx" class="nav-link">Students</a>
                    <a href="assessments.aspx" class="nav-link">Assessments</a>
                    <a href="analytics.aspx" class="nav-link">Analytics</a>
                    <a href="messages.aspx" class="nav-link">Messages</a>
                </nav>
                <div class="nav-profile">
                    <span class="profile-name">Dr. Robert Miller</span>
                    <!-- If avatar is missing, use a placeholder or add tutor-avatar.png to your images folder -->
                    <img src="../../images/tutor-avatar.png" alt="Dr. Robert Miller" class="profile-avatar" onerror="this.style.display='none'" />
                </div>
            </header>

            <!-- Main Dashboard Body -->
            <main class="dashboard-container">
                
                <!-- Top Statistics Overview Cards -->
                <div class="stats-overview-grid">
                    <div class="stat-card">
                        <span class="stat-card-title">Total Students</span>
                        <h2 class="stat-card-value">248</h2>
                        <span class="stat-trend positive">+12 this week</span>
                    </div>
                    <div class="stat-card">
                        <span class="stat-card-title">Active Courses</span>
                        <h2 class="stat-card-value">6</h2>
                        <span class="stat-trend neutral">2 assignments pending</span>
                    </div>
                    <div class="stat-card">
                        <span class="stat-card-title">Average Score</span>
                        <h2 class="stat-card-value">78.4%</h2>
                        <span class="stat-trend positive">+1.2% this cohort</span>
                    </div>
                    <div class="stat-card">
                        <span class="stat-card-title">Completion Rate</span>
                        <h2 class="stat-card-value">64%</h2>
                        <span class="stat-trend neutral">Target: 70%</span>
                    </div>
                </div>

                <!-- Content Split Grid (Left: Courses & Table, Right: Sidebar) -->
                <div class="content-split-grid">
                    
                    <!-- Left Column -->
                    <div class="left-column">
                        
                        <!-- My Courses Overview -->
                        <section class="section-block">
                            <h3 class="section-heading">My Courses Overview</h3>
                            <div class="courses-grid">
                                <div class="course-mini-card">
                                    <div class="course-card-header">
                                        <h4>Intro to Algorithms</h4>
                                        <a href="#" class="action-link">View Details &rarr;</a>
                                    </div>
                                    <div class="course-card-meta">
                                        <span>Enrolled: 84</span>
                                        <span class="progress-text">72%</span>
                                    </div>
                                    <div class="progress-bar-container">
                                        <div class="progress-bar-fill" style="width: 72%;"></div>
                                    </div>
                                </div>

                                <div class="course-mini-card">
                                    <div class="course-card-header">
                                        <h4>Advanced Data Structures</h4>
                                        <a href="#" class="action-link">View Details &rarr;</a>
                                    </div>
                                    <div class="course-card-meta">
                                        <span>Enrolled: 56</span>
                                        <span class="progress-text">42%</span>
                                    </div>
                                    <div class="progress-bar-container">
                                        <div class="progress-bar-fill" style="width: 42%;"></div>
                                    </div>
                                </div>

                                <div class="course-mini-card">
                                    <div class="course-card-header">
                                        <h4>Object-Oriented Architecture</h4>
                                        <a href="#" class="action-link">View Details &rarr;</a>
                                    </div>
                                    <div class="course-card-meta">
                                        <span>Enrolled: 108</span>
                                        <span class="progress-text">61%</span>
                                    </div>
                                    <div class="progress-bar-container">
                                        <div class="progress-bar-fill" style="width: 61%;"></div>
                                    </div>
                                </div>
                            </div>
                        </section>

                        <!-- Active Student Progress Table -->
                        <section class="section-block">
                            <h3 class="section-heading">Active Student Progress</h3>
                            <div class="table-card">
                                <table class="data-table">
                                    <thead>
                                        <tr>
                                            <th>STUDENT NAME</th>
                                            <th>COURSE</th>
                                            <th>PROGRESS</th>
                                            <th>LAST ACTIVE</th>
                                            <th>STATUS</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <tr>
                                            <td><strong>Aarav Mehta</strong></td>
                                            <td>Advanced Data Structures</td>
                                            <td>
                                                <div class="table-progress-wrapper">
                                                    <div class="progress-bar-container small"><div class="progress-bar-fill" style="width: 85%;"></div></div>
                                                    <span>85%</span>
                                                </div>
                                            </td>
                                            <td>10 mins ago</td>
                                            <td><span class="badge status-on-track">ON TRACK</span></td>
                                        </tr>
                                        <tr>
                                            <td><strong>Siddharth Rao</strong></td>
                                            <td>Intro to Algorithms</td>
                                            <td>
                                                <div class="table-progress-wrapper">
                                                    <div class="progress-bar-container small"><div class="progress-bar-fill" style="width: 68%;"></div></div>
                                                    <span>68%</span>
                                                </div>
                                            </td>
                                            <td>2 hrs ago</td>
                                            <td><span class="badge status-on-track">ON TRACK</span></td>
                                        </tr>
                                        <tr>
                                            <td><strong>Nisha Patel</strong></td>
                                            <td>Advanced Data Structures</td>
                                            <td>
                                                <div class="table-progress-wrapper">
                                                    <div class="progress-bar-container small"><div class="progress-bar-fill" style="width: 32%;"></div></div>
                                                    <span>32%</span>
                                                </div>
                                            </td>
                                            <td>1 day ago</td>
                                            <td><span class="badge status-attention">NEEDS ATTENTION</span></td>
                                        </tr>
                                        <tr>
                                            <td><strong>Aditi Rao</strong></td>
                                            <td>Object-Oriented Architecture</td>
                                            <td>
                                                <div class="table-progress-wrapper">
                                                    <div class="progress-bar-container small"><div class="progress-bar-fill" style="width: 91%;"></div></div>
                                                    <span>91%</span>
                                                </div>
                                            </td>
                                            <td>4 mins ago</td>
                                            <td><span class="badge status-on-track">ON TRACK</span></td>
                                        </tr>
                                        <tr>
                                            <td><strong>Vikram Singh</strong></td>
                                            <td>Intro to Algorithms</td>
                                            <td>
                                                <div class="table-progress-wrapper">
                                                    <div class="progress-bar-container small"><div class="progress-bar-fill" style="width: 15%;"></div></div>
                                                    <span>15%</span>
                                                </div>
                                            </td>
                                            <td>3 days ago</td>
                                            <td><span class="badge status-risk">AT RISK</span></td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                        </section>

                    </div>

                    <!-- Right Column: Awaiting Review Sidebar -->
                    <div class="right-column">
                        <section class="section-block">
                            <h3 class="section-heading">Awaiting Review</h3>
                            <div class="review-card-list">
                                <div class="review-item-card">
                                    <div class="review-info">
                                        <h4>Meera Nair</h4>
                                        <p>BST Balancing Lab</p>
                                        <span class="time-ago">2 hrs ago</span>
                                    </div>
                                    <asp:Button ID="BtnGrade1" runat="server" Text="Grade" CssClass="btn-grade" />
                                </div>

                                <div class="review-item-card">
                                    <div class="review-info">
                                        <h4>Dev Patel</h4>
                                        <p>Complexity Homework 2</p>
                                        <span class="time-ago">4 hrs ago</span>
                                    </div>
                                    <asp:Button ID="BtnGrade2" runat="server" Text="Grade" CssClass="btn-grade" />
                                </div>

                                <div class="review-item-card">
                                    <div class="review-info">
                                        <h4>Ananya Sen</h4>
                                        <p>BST Balancing Lab</p>
                                        <span class="time-ago">1 day ago</span>
                                    </div>
                                    <asp:Button ID="BtnGrade3" runat="server" Text="Grade" CssClass="btn-grade" />
                                </div>
                            </div>
                        </section>
                    </div>

                </div>

            </main>
        </div>
    </form>
</body>
</html>