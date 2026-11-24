import ReactMarkdown, { defaultUrlTransform } from 'react-markdown';

/**
 * Markdown seguro para contenido de la API (blog, paquetes…).
 * - No renderiza HTML crudo (react-markdown lo ignora sin rehype-raw).
 * - `defaultUrlTransform` descarta esquemas peligrosos (javascript:, data:…).
 * - Enlaces externos en pestaña nueva con rel="noopener noreferrer"; imágenes perezosas.
 */
export function Markdown({ children, className }: { children: string; className?: string }) {
  return (
    <div className={className}>
      <ReactMarkdown
        skipHtml
        urlTransform={defaultUrlTransform}
        components={{
          a: ({ href, children: text }) => {
            const external = Boolean(href && /^https?:\/\//i.test(href));
            return (
              <a href={href} {...(external ? { target: '_blank', rel: 'noopener noreferrer' } : {})}>
                {text}
              </a>
            );
          },
          img: ({ src, alt }) => (typeof src === 'string' ? <img src={src} alt={alt ?? ''} loading="lazy" decoding="async" /> : null),
        }}
      >
        {children}
      </ReactMarkdown>
    </div>
  );
}
