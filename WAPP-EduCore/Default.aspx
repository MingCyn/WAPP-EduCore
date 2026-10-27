<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="WAPP_EduCore.Default" %>

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <meta name="description" content="EduCore offers interactive synchronous and asynchronous learning for secondary students preparing for SPM and IGCSE Biology, Chemistry, and Physics.">
    <title>EduCore - Master the Core of Science, Excel in Every Exam.</title>
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin="">
    <link href="https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700;800&display=swap" rel="stylesheet">
    <link rel="stylesheet" href="/Content/landing.css">
    <script src="/Content/fx.js"></script>
</head>
<body class="landing-body">

    <!-- 1. NAVIGATION BAR -->
    <nav class="landing-nav" aria-label="Main">
        <div class="nav-container">
            <div class="brand">
                <a href="#top" aria-label="EduCore home">
                    <img src="images/logo.png" alt="EduCore">
                </a>
            </div>
            <div class="nav-links">
                <a href="#explore">Explore Courses</a>
                <a href="#paths">Learning Paths</a>
                <a href="#schools">For Schools</a>
                <a href="#about">About</a>
            </div>
            <div class="nav-auth">
                <a href="/Auth/login.aspx" class="btn-login">Log In</a>
                <a href="/Auth/signup.aspx" class="btn-primary">Get Started Free</a>
            </div>
        </div>
    </nav>

    <!-- 2. HERO SECTION -->
    <header class="landing-hero" id="top">
        <div class="hero-container">
            <div class="hero-content">
                <h1>Mastering SPM & IGCSE science</h1>
                <p>Flexible synchronous and asynchronous learning tailored for high school students. Build knowledge in Biology, Chemistry, and Physics with interactive lessons, practice exercises, and direct tutor support.</p>
                <div class="hero-actions">
                    <a href="#explore" class="btn-primary">Explore Courses</a>
                    <a href="#how-it-works" class="btn-secondary">How It Works</a>
                </div>
            </div>
            <div class="hero-visual">
                <img class="hero-photo" src="images/study.jpg" alt="Students learning science together" width="800" height="640">
                <div class="ill-preview-card">
                    <span class="ill-tag">SPM Biology</span>
                    <h4>Cell Structure & Function</h4>
                    <div class="progress-bar" role="progressbar" aria-valuenow="75" aria-valuemin="0" aria-valuemax="100" aria-label="Course progress">
                        <div class="fill" style="width: 75%;"></div>
                    </div>
                    <p class="progress-label">75% complete</p>
                </div>
                <div class="ill-badge-float">🎓 Verified certificates</div>
            </div>
        </div>
    </header>

    <!-- 3. TRUST AND CREDIBILITY -->
    <section class="trust-section" aria-label="EduCore Platform Highlights">
        <div class="trust-container">
            <div class="stat-box">
                <h2>3 Core Subjects</h2>
                <p>Biology, Chemistry & Physics</p>
            </div>
            <div class="stat-box">
                <h2>SPM & IGCSE</h2>
                <p>Aligned with Exam Curriculums</p>
            </div>
            <div class="stat-box">
                <h2>Sync & Async</h2>
                <p>Learning Sessions & Self-Paced Modules</p>
            </div>
            <div class="stat-box">
                <h2>Interactive</h2>
                <p>Auto-Graded Quizzes & Discussions</p>
            </div>
        </div>
    </section>

<!-- 4. POPULAR COURSES -->
    <section id="explore" class="popular-courses">
    <div class="section-container">
        <div class="section-header-landing">
            <div>
                <h2>Explore popular courses</h2>
                <p>Hand-picked programs to fast-track your science skills.</p>
            </div>
            <%-- Updated: Links to the full course directory --%>
            <a href="pages/Student/myCourses.aspx" class="view-all">View all courses &rarr;</a>
        </div>
        
        <!-- Repeater starts here -->
        <div class="courses-grid-4">
            <asp:Repeater ID="RepeaterCourses" runat="server">
                <ItemTemplate>
                    <%-- Wrapped the card inside an anchor tag lin<a href="Default.aspx">Default.aspx</a>king to courseOverview with the specific course ID --%>
                    <a href="pages/Student/courseOverview.aspx?id=<%# Eval("course_id") %>" class="course-card-link" style="text-decoration: none; color: inherit; display: block;">
                        <article class="course-card">
<div class="course-thumb" style="background-image: url('<%# Eval("course_img") %>'); background-size: cover; background-position: center;"></div>                            <div class="course-body">
                                <span class="category-tag"><%# Eval("category") %></span>
                                <h4><%# Eval("title") %></h4>
                                <span class="tutor"><%# Eval("tutor_name") %></span>
                                <div class="course-meta">
                                    <span class="rating">★ 4.9</span>
                                    <span class="duration"><%# Eval("duration") %></span>
                                </div>
                            </div>
                        </article>
                    </a>
                </ItemTemplate>
            </asp:Repeater>
        </div>
        <!-- Repeater ends here -->
    </div></section>

    <!-- 5. HOW IT WORKS -->
    <section id="how-it-works" class="how-it-works">
        <div class="section-container">
            <div class="section-header-landing">
                <div>
                    <h2>How EduCore Works</h2>
                    <p>Master your science subjects in three simple steps.</p>
                </div>
            </div>
            <div class="steps-grid">
                <div class="step">
                    <h3>Select Your Subject</h3>
                    <p>Choose from Biology, Chemistry, or Physics structured by SPM or IGCSE topics.</p>
                </div>
                <div class="step">
                    <h3>Learn & Practice</h3>
                    <p>Watch synchronous sessions or learn asynchronously with interactive quizzes and immediate feedback.</p>
                </div>
                <div class="step">
                    <h3>Track Progress</h3>
                    <p>Monitor your learning hours, streaks, and quiz performance to sharpen your exam readiness.</p>
                </div>
            </div>
        </div>
    </section>

    <!-- 6. LEARNING PATHS -->
    <section id="paths" class="learning-paths">
        <div class="section-container">
            <div class="section-header-landing">
                <div>
                    <h2>Curriculum Learning Paths</h2>
                    <p>Follow a structured sequence built around examination requirements.</p>
                </div>
            </div>
            <div class="paths-grid">
                <div class="path-card">
                    <h3>SPM Science Masterclass</h3>
                    <p>Complete coverage of Form 4 and Form 5 Biology, Chemistry, and Physics syllabus.</p>
                    <span class="path-meta">SPM Curriculum</span>
                </div>
                <div class="path-card">
                    <h3>Physics & Chemistry Intensive</h3>
                    <p>Focus on problem-solving, equations, and practice exercises for physical sciences.</p>
                    <span class="path-meta">Subject Specialization</span>
                </div>
                <div class="path-card">
                    <h3>IGCSE Science Mastery</h3>
                    <p>Targeted core and extended topics across Cambridge IGCSE science subjects.</p>
                    <span class="path-meta">IGCSE Curriculum</span>
                </div>
            </div>
        </div>
    </section>

    <!-- 7. REGISTRATION BANNER -->
    <section class="reg-banner" id="schools">
        <div class="reg-content">
            <h2>Start Your Science Learning Journey Today</h2>
            <p>Join high school students mastering Biology, Chemistry, and Physics on EduCore.</p>
            <a href="/Auth/signup.aspx" class="btn-primary-large">Create Your Free Account</a>
            <p class="reg-note">Teaching at a school? <a href="/Auth/signup.aspx">Contact us about school plans</a></p>
        </div>
    </section>

    <footer class="landing-footer" id="about">
        <div class="footer-container">
            <div class="footer-col">
                <h3>EduCore</h3>
                <p>Empowering high school students through structured, interactive science learning and tutor support.</p>
            </div>
            <div class="footer-col">
                <h4>Course links</h4>
                <a href="#">Biology</a>
                <a href="#">Chemistry</a>
                <a href="#">Physics</a>
                <a href="#">SPM Science Prep</a>
            </div>
            <div class="footer-col">
                <h4>Learning resources</h4>
                <a href="#">Interactive Quizzes</a>
                <a href="#">Discussion Forums</a>
                <a href="#">Study Calendar</a>
                <a href="#">Progress Analytics</a>
            </div>
            <div class="footer-col">
                <h4>Support & Roles</h4>
                <a href="#">Student Portal</a>
                <a href="#">Tutor Portal</a>
                <a href="#">Administrator Hub</a>
                <a href="#">Help Center</a>
            </div>
        </div>
        <div class="footer-bottom">
            <p>@ 2026 EduCore Technologies. All rights reserved.</p>
        </div>
    </footer>

</body>
</html>