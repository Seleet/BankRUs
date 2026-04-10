import { useState } from 'react'
import './App.css'

function App() {
  const [socialSecurityNumber, setSocialSecurityNumber] = useState('')
  const [amount, setAmount] = useState('')
  const [loading, setLoading] = useState(false)
  const [decision, setDecision] = useState('')
  const [error, setError] = useState('')

  const apiBaseUrl = import.meta.env.VITE_LOAN_API_URL ?? 'https://localhost:7001'

  const onSubmit = async (event) => {
    event.preventDefault()
    setDecision('')
    setError('')
    setLoading(true)

    try {
      const response = await fetch(`${apiBaseUrl}/api/loanapplications`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          socialSecurityNumber,
          amount: Number(amount),
        }),
      })

      if (!response.ok) {
        const problemDetails = await response.json().catch(() => null)
        if (problemDetails?.errors) {
          const formattedError = Object.values(problemDetails.errors)
            .flat()
            .join(' ')
          throw new Error(formattedError)
        }

        throw new Error('Något gick fel när ansökan skickades.')
      }

      const data = await response.json()
      setDecision(data.Status ?? data.status ?? 'Unknown')
    } catch (submitError) {
      setError(submitError.message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <main className="container">
      <h1>Bank-R-Us</h1>
      <p className="description">Ansök om lån genom att fylla i det fina formuläret nedan.</p>

      <form onSubmit={onSubmit} className="loan-form">
        <label htmlFor="ssn">Personnummer</label>
        <input
          id="ssn"
          type="text"
          placeholder="YYYYMMDD-XXXX"
          value={socialSecurityNumber}
          onChange={(event) => setSocialSecurityNumber(event.target.value)}
          required
        />

        <label htmlFor="amount">Belopp</label>
        <input
          id="amount"
          type="number"
          min="1"
          step="1"
          placeholder="100000"
          value={amount}
          onChange={(event) => setAmount(event.target.value)}
          required
        />

        <button type="submit" disabled={loading}>
          {loading ? 'Skickar...' : 'Ansök'}
        </button>
      </form>

      {decision && <p className="decision">Status: {decision}</p>}
      {error && <p className="error">{error}</p>}
    </main>
  )
}

export default App
