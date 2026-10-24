export type Inline = { t: 'text'; v: string } | { t: 'link'; v: string; href: string; external: boolean }
export type Block = { type: 'h2'; text: string } | { type: 'p'; inline: Inline[] } | { type: 'ul'; items: Inline[][] }
export function parseInline(text: string): Inline[]
export function parseBody(text: string): Block[]
export function plainText(blocks: Block[]): string
export function readMinutes(text: string): number
export function linkedGuideSlugs(text: string): string[]
