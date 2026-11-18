<template>
  <main class="access-denied">
    <div class="icon" aria-hidden="true">!</div>

    <h1>Geen toegang</h1>
    <p>
      Je hebt geen rechten om deze pagina te bekijken.
      Je kunt teruggaan naar je eigen omgeving.
    </p>

    <RouterLink class="back-button" to="/">
      Terug naar mijn omgeving
    </RouterLink>
  </main>
</template>

<style scoped>
.access-denied {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 24px;
  background: #eaf3fc;
  color: #153b70;
  text-align: center;
  font-family: Arial, sans-serif;
}

.icon {
  width: 54px;
  height: 54px;
  margin-bottom: 34px;
  border: 4px solid #153b70;
  border-radius: 50%;
  font-size: 40px;
  line-height: 46px;
}

h1 {
  margin: 0 0 12px;
  font-size: 17px;
}

p {
  max-width: 300px;
  margin: 0 0 44px;
  font-size: 11px;
}

.back-button {
  width: min(100%, 280px);
  padding: 6px 12px;
  border-radius: 8px;
  background: #153b70;
  color: white;
  font-size: 16px;
  font-weight: 700;
  text-decoration: none;
}

.back-button:hover {
  background: #24528f;
}
</style>