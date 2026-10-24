import { SITE } from '../site'
import { useSeo } from '../seo'
import { setConsent, useConsent } from '../consent'

/** Starting-point privacy policy. Have it reviewed for your jurisdiction (GDPR/UK GDPR/CCPA/COPPA) and fill in your legal entity. */
export default function Privacy() {
  const consent = useConsent()
  useSeo({ title: `Privacy policy – ${SITE.name}`, description: `How ${SITE.name} handles your data, cookies, advertising and your child's tracking information.`, path: '/privacy' })
  return (
    <article className="page prose">
      <h1>Privacy policy</h1>
      <p><small>Last updated: October 2026</small></p>
      <p>{SITE.name} helps parents track potty training. This policy explains what we collect and why. Questions: <a href={`mailto:${SITE.contactEmail}`}>{SITE.contactEmail}</a>.</p>
      <h2>What we collect</h2>
      <ul>
        <li><b>Account:</b> your email, display name and a hashed password.</li>
        <li><b>Child tracking data you enter:</b> a name or nickname, birth date, and logs such as pees, poops, accidents, drinks and food. This is your data; we use it only to run the app, show your insights and let you share with caregivers you invite.</li>
        <li><b>Website:</b> if you join the waitlist we store your email. Our servers keep standard technical logs.</li>
        <li><b>Contributions:</b> optional one-off payments are handled by Stripe. We receive only the amount and a transaction reference, never your card details.</li>
      </ul>
      <h2>Advertising, analytics and cookies</h2>
      <p>The public website (for example the guides) may use <b>analytics</b> (the Google tag) and <b>advertising</b> (Google AdSense and, if enabled, marketing tags such as the Meta Pixel). None of them load until you opt in, and you can choose each category separately. Allowing ads is optional and is one way to support us; a contribution is another. Ads are requested as non-personalized.</p>
      <p><b>None of these tools are used inside the child tracking area or on shared report pages, and we never share your child's data, your logs or your account details with advertisers or analytics providers.</b> Website events contain only things like which page was viewed or that a waitlist form was submitted, never names, emails or health data.</p>
      <p>Your choices: analytics <b>{consent.analytics ? 'allowed' : 'off'}</b>, advertising &amp; marketing <b>{consent.advertising ? 'allowed' : 'off'}</b>.{' '}
        <button className="link" onClick={() => setConsent(false, false)}>Turn both off</button> · <button className="link" onClick={() => setConsent(true, true)}>Allow both</button></p>
      <p>If you opened a link with campaign parameters (such as <code>utm_source</code>) and joined the waitlist, we store those parameters with your sign-up to learn which channels work.</p>
      <h2>Reports and shared links</h2>
      <p>You can generate a report of your child's log and, if you choose, share it through a private link. Links are unguessable, expire (you choose when, up to 30 days), can be revoked at any time, are not indexed by search engines, and anyone holding the link can view that report until it expires or you revoke it.</p>
      <h2>Your rights</h2>
      <p>You can ask us to export or delete your account and your child's data at any time by emailing us. Deleting a child's profile removes its logs.</p>
      <h2>Security and retention</h2>
      <p>Passwords are hashed, connections use HTTPS, and sessions can be ended by signing out. We keep your data until you delete your account.</p>
      <h2>Children</h2>
      <p>{SITE.name} is for parents and caregivers, not for children to use. We do not knowingly collect information from children.</p>
      <h2>Not medical advice</h2>
      <p>Insights and guides are informational and are not a substitute for professional medical advice.</p>
    </article>
  )
}
