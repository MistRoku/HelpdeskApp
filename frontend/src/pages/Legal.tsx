import React from 'react';

export function Tos() {
  return (
    <div className="legal">
      <h1>Terms of Service</h1>
      <p>Last updated: 30 September 2026. These terms govern use of the Helpdesk Ticketing System.</p>
      <ol>
        <li>Service scope: the system provides ticket intake, knowledge articles, SLA tracking, and satisfaction surveys for support teams and their customers.</li>
        <li>Acceptable use: you agree not to submit unlawful content, not to attempt unauthorized access, and not to submit instruction override prompts aimed at the AI triage feature. Such prompts are blocked and logged.</li>
        <li>Accounts: you are responsible for credentials. Password changes revoke all other sessions. Reset links are single use and expire after 60 minutes.</li>
        <li>Fair use: automated classification is capped per user per day. Excess use returns a limit notice.</li>
        <li>Content: knowledge articles and replies you submit must be accurate to your knowledge. Do not include payment card data or passwords in tickets.</li>
        <li>Availability: targets follow stated SLA policies during business hours Mon to Fri, 08:00 to 17:00 SAST, excluding public holidays.</li>
        <li>Limitation: the service is provided as is. To the extent permitted by law, liability is limited to fees paid in the prior 3 months.</li>
        <li>Contact: support team via the Tickets page for questions about these terms.</li>
      </ol>
    </div>
  );
}

export function Privacy() {
  return (
    <div className="legal">
      <h1>Privacy Policy</h1>
      <p>Last updated: 30 September 2026. This policy explains what we collect and why.</p>
      <ol>
        <li>Data collected: account identifiers, ticket content, replies, attachments, article feedback, survey scores, and security logs such as reset attempts and rate limit events.</li>
        <li>Use: to operate the helpdesk, enforce SLAs, improve articles, measure satisfaction, and protect against abuse.</li>
        <li>Sanitization: submitted text is sanitized before storage to remove scripts and active content.</li>
        <li>Cookies: strictly necessary session and CSRF cookies only. They use HttpOnly, Secure, and SameSite Strict flags. No advertising cookies.</li>
        <li>Retention: tickets and feedback are kept while the account is active and per legal duties, then deleted or anonymized.</li>
        <li>Sharing: data is not sold. Processors such as hosting and email delivery act only on instruction.</li>
        <li>Rights: request access, correction, or deletion via the support team. Reset links expire and cannot be reused.</li>
        <li>Security: HSTS, locked down CORS, rate limits, JWT short expiry with refresh rotation, and session revocation on password change are enabled.</li>
      </ol>
    </div>
  );
}
