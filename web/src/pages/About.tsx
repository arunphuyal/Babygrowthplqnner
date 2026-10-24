import { Link } from 'react-router-dom'
import { SITE } from '../site'
import { useSeo } from '../seo'

/** About and editorial policy. Keep this honest: only claim reviews and credentials that are true, and add the company details after registration. */
export default function About() {
  useSeo({ title: `About ${SITE.name} and how we write our guides`, description: `${SITE.name} is a free tracker and guide library for parents of babies and toddlers, starting with potty training. Learn who we are and how we write.`, path: '/about' })
  return (
    <article className="page prose">
      <h1>About {SITE.name}</h1>
      <p>{SITE.name} helps parents of babies and toddlers track the small daily things that add up to big milestones. We are starting with potty training: logging pees, poops, accidents, drinks and food, spotting patterns, and sharing a clear summary with partners, daycare or a doctor. The app is free for every family.</p>
      <h2>How we pay for it</h2>
      <p>{SITE.name} is funded by optional contributions from parents and, if you allow them, ads on this website's guide pages. There are never ads inside the tracking app or next to your child's information. Contributing and allowing ads are both optional, and you can change your ad choice at any time using "Ad &amp; cookie choices" in the footer.</p>
      <h2>How we write our guides</h2>
      <ul>
        <li>Guides are written for parents in plain language and cover general, widely accepted guidance.</li>
        <li>They are information, not medical advice. Every health-related guide tells you when to talk to your doctor, and we never recommend medicines or doses.</li>
        <li>We update guides when we learn something new, and each guide shows when it was last changed in its page data.</li>
        <li>We do not accept payment to recommend products. If we ever add affiliate links we will label them clearly.</li>
      </ul>
      <h2>Corrections and contact</h2>
      <p>If you spot a mistake or have a question, email <a href={`mailto:${SITE.contactEmail}`}>{SITE.contactEmail}</a>. We read every message and correct errors promptly.</p>
      <p>Read our <Link to="/privacy">privacy policy</Link> or browse the <Link to="/guides">potty training guides</Link>.</p>
    </article>
  )
}
