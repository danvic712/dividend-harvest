export function PageTitle({ eyebrow, title, description, actions }: { eyebrow: string; title: string; description?: string; actions?: React.ReactNode }) {
  return (
    <div className="page-title-row">
      <div>
        <p className="eyebrow">{eyebrow}</p>
        <h1 className="page-title">{title}</h1>
        {description && <p className="page-description">{description}</p>}
      </div>
      {actions && <div className="page-actions">{actions}</div>}
    </div>
  )
}

export function SectionHeading({ label, title, description }: { label?: string; title: string; description?: string }) {
  return (
    <div className="section-heading">
      {label && <p className="eyebrow">{label}</p>}
      <h2>{title}</h2>
      {description && <p>{description}</p>}
    </div>
  )
}
