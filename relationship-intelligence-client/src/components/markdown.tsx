import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";

// Consistent assistant-message rendering: headings, lists, emphasis, and
// GitHub tables share one visual language everywhere in the product.
// Raw HTML is never rendered (no rehype plugins by design).
export function AssistantMarkdown({ text }: { text: string }) {
  return (
    <div className="assistant-md flex min-w-0 flex-col gap-1.5 text-sm leading-relaxed">
      <ReactMarkdown
        remarkPlugins={[remarkGfm]}
        components={{
          p: ({ children }) => <p className="whitespace-pre-wrap">{children}</p>,
          h1: ({ children }) => <p className="font-semibold">{children}</p>,
          h2: ({ children }) => <p className="font-semibold">{children}</p>,
          h3: ({ children }) => <p className="text-[13px] font-semibold uppercase tracking-wide text-muted-foreground">{children}</p>,
          h4: ({ children }) => <p className="text-[13px] font-semibold uppercase tracking-wide text-muted-foreground">{children}</p>,
          ul: ({ children }) => <ul className="flex list-disc flex-col gap-0.5 pl-4">{children}</ul>,
          ol: ({ children }) => <ol className="flex list-decimal flex-col gap-0.5 pl-4">{children}</ol>,
          li: ({ children }) => <li>{children}</li>,
          strong: ({ children }) => <strong className="font-semibold">{children}</strong>,
          em: ({ children }) => <em>{children}</em>,
          code: ({ children }) => <code className="rounded bg-secondary px-1 py-0.5 font-mono text-[12px]">{children}</code>,
          pre: ({ children }) => <pre className="overflow-x-auto rounded-md bg-secondary p-2 font-mono text-[12px]">{children}</pre>,
          blockquote: ({ children }) => <blockquote className="border-l-2 pl-2 text-muted-foreground">{children}</blockquote>,
          hr: () => <hr className="border-border" />,
          a: ({ children, href }) => (
            <a className="underline" href={href} target="_blank" rel="noreferrer">
              {children}
            </a>
          ),
          table: ({ children }) => (
            <div className="overflow-x-auto rounded-md border">
              <table className="w-full border-collapse text-[13px]">{children}</table>
            </div>
          ),
          thead: ({ children }) => <thead className="bg-secondary/60">{children}</thead>,
          th: ({ children }) => (
            <th className="border-b px-2 py-1 text-left font-semibold">{children}</th>
          ),
          td: ({ children }) => <td className="border-b px-2 py-1 align-top last:border-b-0">{children}</td>,
          tr: ({ children }) => <tr className="last:[&>td]:border-b-0">{children}</tr>,
        }}
      >
        {text}
      </ReactMarkdown>
    </div>
  );
}
