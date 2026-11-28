<template>
  <section class="access-denied" aria-labelledby="page-title">
    <div class="icon" aria-hidden="true">!</div>

    <h1
      id="page-title"
      tabindex="-1"
      aria-describedby="access-description"
    >
      Geen toegang
    </h1>

    <p id="access-description" tabindex="0">
      Uw account heeft geen systeembeheerrechten en kan deze pagina daarom
      niet bekijken.
    </p>
   

    <RouterLink class="back-button" to="/">
      Terug naar mijn dashboard
    </RouterLink>
  </section>
</template>

<script setup>
import { onMounted, onUnmounted } from 'vue'

let focusTimer

onMounted(() => {
  focusTimer = window.setTimeout(() => {
    document.getElementById('page-title')?.focus()
  }, 100)
})

onUnmounted(() => {
  window.clearTimeout(focusTimer)
})
</script>

<style scoped>
.access-denied {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: clamp(24px, 6vw, 48px);
  background: #eaf3fc;
  color: #153b70;
  font-family: Arial, sans-serif;
  text-align: center;
}

.icon {
  width: 56px;
  height: 56px;
  margin-bottom: 28px;
  border: 4px solid currentColor;
  border-radius: 50%;
  font-size: 40px;
  line-height: 48px;
}

h1 {
  margin: 0 0 16px;
  font-size: 1.5rem;
}

p {
  max-width: 36rem;
  margin: 0 0 12px;
  font-size: 1rem;
  line-height: 1.5;
}

#access-description:focus-visible {
  outline: 3px solid #153b70;
  outline-offset: 4px;
  border-radius: 3px;
}

.back-button {
  width: min(100%, 18rem);
  margin-top: 24px;
  padding: 12px 16px;
  border-radius: 8px;
  background: #153b70;
  color: #fff;
  font-size: 1rem;
  font-weight: 700;
  text-align: center;
  text-decoration: none;
}

.back-button:hover {
  background: #24528f;
}

.back-button:focus-visible {
  outline: 3px solid #153b70;
  outline-offset: 4px;
}
</style>
