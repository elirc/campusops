import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './style.css'

function App() {
  return <main><p className="eyebrow">CampusOps</p><h1>Facilities work, made visible.</h1><p>The operations dashboard arrives in Sprint 13.</p></main>
}

createRoot(document.getElementById('app')!).render(<StrictMode><App /></StrictMode>)
