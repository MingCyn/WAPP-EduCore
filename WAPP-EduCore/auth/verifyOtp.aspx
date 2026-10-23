<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="verifyOtp.aspx.cs" Inherits="WAPP_EduCore.pages.Auth.verifyOtp" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>EduCore - Verify OTP</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet" />
    <link href="../../Content/dashboard.css" rel="stylesheet" type="text/css" />
</head>
<body style="display: flex; align-items: center; justify-content: center; height: 100vh; background-color: #f8fafc;">
    <form id="form1" runat="server" style="background: white; padding: 32px; border-radius: 12px; border: 1px solid #e2e8f0; width: 400px;">
        <h2 style="margin-bottom: 8px; color: #0f172a;">Enter OTP & New Password</h2>
        <p style="color: #64748b; font-size: 14px; margin-bottom: 24px;">Check your email inbox for the 6-digit code.</p>
        
        <div style="margin-bottom: 16px;">
            <label style="display: block; font-size: 13px; font-weight: 600; color: #334155; margin-bottom: 6px;">6-Digit OTP</label>
            <asp:TextBox ID="txtOtp" runat="server" CssClass="form-control" style="width: 100%; padding: 10px; border: 1px solid #cbd5e1; border-radius: 6px;" placeholder="123456"></asp:TextBox>
        </div>

        <div style="margin-bottom: 16px;">
            <label style="display: block; font-size: 13px; font-weight: 600; color: #334155; margin-bottom: 6px;">New Password</label>
            <asp:TextBox ID="txtNewPassword" runat="server" CssClass="form-control" TextMode="Password" style="width: 100%; padding: 10px; border: 1px solid #cbd5e1; border-radius: 6px;" placeholder="••••••••"></asp:TextBox>
        </div>

        <asp:Button ID="btnReset" runat="server" Text="Update Password" OnClick="btnReset_Click" style="width: 100%; background-color: #1e3a8a; color: white; border: none; padding: 10px; font-weight: 600; border-radius: 6px; cursor: pointer;" />
        
        <asp:Label ID="lblMessage" runat="server" style="display: block; margin-top: 16px; font-size: 13px;" />
    </form>
</body>
</html>