# Email Setup Guide for Waitlist Notifications

## Current Status
✅ **The waitlist system is working correctly!**

When you check the database, you can see:
- Users are being added to the waitlist when rooms are unavailable
- When rooms become available, the system automatically processes the waitlist
- EmailSentAt timestamps show that notifications are being triggered

## Why You're Not Receiving Emails

The email service is running in **simulation mode** because no SMTP configuration is provided. This means:
- Email notifications are being logged to the console
- The system marks emails as "sent" (EmailSentAt is recorded)
- But no actual email is delivered to your inbox

## How to Receive Real Emails

### Option 1: Gmail (Recommended for Testing)

1. **Enable 2-Factor Authentication** on your Gmail account

2. **Generate an App Password**:
   - Go to: https://myaccount.google.com/apppasswords
   - Select "Mail" and "Windows Computer"
   - Click "Generate"
   - Copy the 16-character password

3. **Update appsettings.Development.json**:
```json
"Email": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": "587",
  "Username": "your-email@gmail.com",
  "Password": "your-16-char-app-password",
  "FromEmail": "your-email@gmail.com",
  "FromName": "MVC Travel Booking"
}
```

### Option 2: SendGrid (Free Tier - 100 emails/day)

1. **Sign up**: https://sendgrid.com/
2. **Get API Key**: Settings → API Keys → Create API Key
3. **Update appsettings.Development.json**:
```json
"Email": {
  "SmtpHost": "smtp.sendgrid.net",
  "SmtpPort": "587",
  "Username": "apikey",
  "Password": "YOUR_SENDGRID_API_KEY",
  "FromEmail": "your-verified-email@domain.com",
  "FromName": "MVC Travel Booking"
}
```

### Option 3: AWS SES (Most Reliable)

1. **Sign up**: https://aws.amazon.com/ses/
2. **Verify email address** in AWS Console
3. **Get SMTP credentials**
4. **Update appsettings.Development.json**:
```json
"Email": {
  "SmtpHost": "email-smtp.us-east-1.amazonaws.com",
  "SmtpPort": "587",
  "Username": "YOUR_AWS_SMTP_USERNAME",
  "Password": "YOUR_AWS_SMTP_PASSWORD",
  "FromEmail": "your-verified-email@domain.com",
  "FromName": "MVC Travel Booking"
}
```

## Testing the Email System

After configuring SMTP:

1. **Restart the application**
2. **Create a test scenario**:
   - User A adds all available rooms to cart
   - User B tries to book → gets added to waitlist
   - User A removes rooms from cart
   - User B should receive an email notification

3. **Check the logs**:
   - Look for: `Email sent successfully to [email]`
   - No longer see: `[SIMULATED EMAIL]`

## Viewing Simulation Mode Logs

Even without SMTP configured, you can see when emails would be sent by checking:

1. **Console Output**: Look for `[SIMULATED EMAIL]` messages
2. **Visual Studio Output Window**: Debug → Windows → Output
3. **Application Logs**: Information level logs will show email attempts

## Email Template Preview

The waitlist notification email includes:
- Personalized greeting with user's name
- Trip destination
- Message that trip is already added to cart
- 24-hour deadline reminder
- Direct link to complete payment
- Professional HTML formatting

## Troubleshooting

**"Authentication failed"**: 
- Gmail: Make sure you're using an App Password, not your regular password
- SendGrid: Use "apikey" as username (literally the word "apikey")

**"Relay access denied"**:
- Your SMTP server may require authentication
- Check that Username and Password are correctly set

**Still not working?**:
- Check Windows Firewall isn't blocking outbound port 587
- Try port 465 with SSL instead
- Enable less secure apps (Gmail) - though App Password is better

## Security Note

⚠️ **Never commit appsettings.Development.json with real credentials to Git!**

Add to .gitignore:
```
appsettings.Development.json
appsettings.Production.json
```
