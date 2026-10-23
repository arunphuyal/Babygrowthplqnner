import { useEffect } from 'react'
import { SITE } from './site'

export interface SeoProps {
  title: string
  description?: string
  path: string
  type?: 'website' | 'article'
  noindex?: boolean
  jsonLd?: object | object[]
  publishedTime?: string
  modifiedTime?: string
}

function setMeta(attr: 'name' | 'property', key: string, content: string | null) {
  let el = document.head.querySelector<HTMLMetaElement>(`meta[${attr}="${key}"]`)
  if (content === null) { el?.remove(); return }
  if (!el) { el = document.createElement('meta'); el.setAttribute(attr, key); document.head.appendChild(el) }
  el.setAttribute('content', content)
}

/** Sets the title, description, canonical URL, Open Graph / Twitter tags and JSON-LD for the current page. The prerender script writes the same tags into static HTML for crawlers. */
export function useSeo({ title, description = SITE.description, path, type = 'website', noindex, jsonLd, publishedTime, modifiedTime }: SeoProps) {
  const ld = JSON.stringify(jsonLd ?? null)
  useEffect(() => {
    const url = SITE.url + path
    document.title = title
    setMeta('name', 'description', description)
    setMeta('name', 'robots', noindex ? 'noindex,nofollow' : 'index,follow,max-image-preview:large')
    let link = document.head.querySelector<HTMLLinkElement>('link[rel="canonical"]')
    if (!link) { link = document.createElement('link'); link.rel = 'canonical'; document.head.appendChild(link) }
    link.href = url
    setMeta('property', 'og:title', title)
    setMeta('property', 'og:description', description)
    setMeta('property', 'og:url', url)
    setMeta('property', 'og:type', type)
    setMeta('property', 'og:site_name', SITE.name)
    setMeta('name', 'twitter:card', 'summary')
    setMeta('property', 'article:published_time', publishedTime ?? null)
    setMeta('property', 'article:modified_time', modifiedTime ?? null)
    let script = document.head.querySelector<HTMLScriptElement>('script#ld-json')
    if (ld !== 'null') {
      if (!script) { script = document.createElement('script'); script.id = 'ld-json'; script.type = 'application/ld+json'; document.head.appendChild(script) }
      script.textContent = ld
    } else script?.remove()
  }, [title, description, path, type, noindex, ld, publishedTime, modifiedTime])
}

export const organizationLd = () => ({
  '@context': 'https://schema.org', '@type': 'Organization', name: SITE.name, url: SITE.url, email: SITE.contactEmail,
})

export const websiteLd = () => ({ '@context': 'https://schema.org', '@type': 'WebSite', name: SITE.name, url: SITE.url, description: SITE.description })

export const faqLd = (faqs: string[][]) => ({
  '@context': 'https://schema.org', '@type': 'FAQPage',
  mainEntity: faqs.map(([q, a]) => ({ '@type': 'Question', name: q, acceptedAnswer: { '@type': 'Answer', text: a } })),
})

export const articleLd = (a: { title: string; description: string; slug: string; created?: string; updated?: string }) => ([
  {
    '@context': 'https://schema.org', '@type': 'Article', headline: a.title, description: a.description,
    mainEntityOfPage: `${SITE.url}/guides/${a.slug}`, datePublished: a.created, dateModified: a.updated ?? a.created,
    author: { '@type': 'Organization', name: SITE.name }, publisher: { '@type': 'Organization', name: SITE.name },
  },
  {
    '@context': 'https://schema.org', '@type': 'BreadcrumbList',
    itemListElement: [
      { '@type': 'ListItem', position: 1, name: 'Home', item: SITE.url + '/' },
      { '@type': 'ListItem', position: 2, name: 'Guides', item: SITE.url + '/guides' },
      { '@type': 'ListItem', position: 3, name: a.title, item: `${SITE.url}/guides/${a.slug}` },
    ],
  },
])
