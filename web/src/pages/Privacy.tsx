import { useEffect } from 'react'
import { SITE } from '../site'
import { setConsent, useConsent } from '../consent'

/** Starting-point privacy policy. Have it reviewed for your jurisdiction (GDPR/UK GDPR/CCPA/COPPA) and fill in your legal entity. */
export default function Privacy() {
  const consent = useConsent()
  useEffect(() => { document.title = 'Privacy policy – PottyTrain' }, [])
  return (
    <article className="page prose">
      <h1>Privacy policy</h1>
      <p><small>Last updated: October 2026</small></p>
      <p>{SITE.name} helps parents track potty training. This policy explains what we collect and why. Questions: <a href={`mailto:${SITE.contactEmail}`}>{SITE.contactEmail}</a>.</p>
      <h2>What we collect</h2>
      <ul>
        <li><b>Account:</b> your email, display name and a hashed password.</li>
        <li><b>Child tracking data you enter:</b> a name or nickname, birth date, and logs such as wees, poos, accidents, drinks and food. This is your data; we use it only to run the app, show your insights and let you share with caregivers you invite.</li>
        <li><b>Website:</b> if you join the waitlist we store your email. Our servers keep standard technical logs.</li>
        <li><b>Donations:</b> payments are handled by Stripe. We receive only the amount and a transaction reference, never your card details.</li>
      </ul>
      <h2>Advertising</h2>
      <p>The public website (for example the guides) may show ads from Google AdSense. With your permission, Google and its partners may use cookies to serve and measure ads; we ask you before loading any ads and we request non-personalised ads. <b>There are no ads in the child tracking area, and we never share your child's data with advertisers.</b></p>
      <p>Your ad choice: <b>{consent === 'granted' ? 'ads allowed' : consent === 'denied' ? 'ads declined' : 'not chosen yet'}</b>.{' '}
        <button className="link" onClick={() => setConsent('denied')}>Decline ads</button> · <button className="link" onClick={() => setConsent('granted')}>Allow ads</button></p>
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
