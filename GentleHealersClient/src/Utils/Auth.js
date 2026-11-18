export function getTokenClaims() {
  const token = localStorage.getItem('token')
  if (!token) return null

  try {
    const payloadPart = token.split('.')[1]
    const base64 = payloadPart
      .replace(/-/g, '+')
      .replace(/_/g, '/')
      .padEnd(Math.ceil(payloadPart.length / 4) * 4, '=')

    const claims = JSON.parse(atob(base64))

    if (claims.exp && claims.exp * 1000 <= Date.now()) {
      return null
    }

    return claims
  } catch {
    return null
  }
}