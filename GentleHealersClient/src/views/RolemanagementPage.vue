<template>
  <div class="app">
    <header class="topbar">
      <button class="menu-button" aria-label="Menu">☰ <span>Menu</span></button>
      <div class="brand">Zachte <strong>Heelmeesters</strong></div>
      <button class="profile">
        <span>Naam<br />nummer<br />geboortedatum</span>
        <span>⌄</span>
      </button>
      <button class="logout" aria-label="Uitloggen">↪</button>
    </header>

    <div class="content">
      <h1 id="page-title" tabindex="-1">Rollen en rechten</h1>
      <p class="intro">Wijzig de rol van een bestaand gebruikersaccount.</p>

      <p
        class="message success"
        :class="{ 'message-empty': !successMessage }"
        role="status"
        aria-live="polite"
        aria-atomic="true"
      >
        {{ successMessage }}
      </p>
      <p
        class="message error"
        :class="{ 'message-empty': !errorMessage }"
        role="alert"
        aria-live="assertive"
        aria-atomic="true"
      >
        {{ errorMessage }}
      </p>

      <div class="search-bar">
        <label for="user-search">Zoek gebruiker op naam</label>
        <input
          id="user-search"
          v-model="searchQuery"
          type="search"
          placeholder="Typ een voor- of achternaam"
          autocomplete="off"
        />
      </div>

      <section class="user-list" aria-labelledby="users-heading">
        <h2 id="users-heading" class="visually-hidden">Gebruikers en huidige rollen</h2>
        <div class="list-heading">
          <span>Gebruiker</span>
          <span>Huidige rol</span>
          <span></span>
          <span></span>
        </div>

        <div v-for="user in filteredUsers" :key="user.email" class="user-row">
          <div class="user-info">
            <span>{{ user.firstName }} {{ user.lastName }}</span>
            <span>{{ user.email }}</span>
          </div>

          <select v-model="user.roleId" :aria-label="`Rol voor ${user.firstName} ${user.lastName}`">
          <option v-for="role in roles" :key="role.roleId" :value="role.roleId">
            {{ role.name }}
          </option>
        </select>

        <button
          class="button button-primary"
          :aria-label="`Rol wijzigen voor ${user.firstName} ${user.lastName}`"
          @click="changeRole(user)"
        >
          Rol wijzigen
        </button>
          <button
            class="button button-danger"
            :aria-label="`Rechten intrekken voor ${user.firstName} ${user.lastName}`"
            @click="revokeAccess(user)"
          >
            Rechten intrekken
          </button>
        </div>

        <p v-if="filteredUsers.length === 0" class="empty-state">
          {{ searchQuery.trim() ? 'Geen gebruikers gevonden met deze naam.' : 'Er zijn geen gebruikers beschikbaar.' }}
        </p>
      </section>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import api from '@/services/api'

const roles = ref([])
const users = ref([])
const searchQuery = ref('')
const errorMessage = ref('')
const successMessage = ref('')

const filteredUsers = computed(() => {
  const query = normalizeName(searchQuery.value.trim())
  if (!query) return users.value

  return users.value.filter((user) =>
    normalizeName(`${user.firstName ?? ''} ${user.lastName ?? ''}`).includes(query),
  )
})

function normalizeName(value) {
  return value
    .toLocaleLowerCase('nl')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
}

onMounted(async () => {
  try {
    const [rolesResponse, usersResponse] = await Promise.all([
      api.get('/admin/roles/roles'),
      api.get('/admin/roles/users'),
    ])

    roles.value = rolesResponse.data
    users.value = usersResponse.data
  } catch (error) {
    errorMessage.value = 'De gebruikers of rollen konden niet worden geladen. Controleer je verbinding en vernieuw de pagina.'
    console.error(error)
  }
})

async function changeRole(user) {
  successMessage.value = ''
  errorMessage.value = ''

  try {
    await api.put(`/admin/roles/user/${user.userId}/role`, {
      roleId: user.roleId,
    })

    successMessage.value =
  `De rol van ${user.firstName} ${user.lastName} is succesvol gewijzigd.` 
  } catch (error) {
    errorMessage.value =
      error.response?.data || 'De rol kon niet worden gewijzigd. Probeer het opnieuw.'
    console.error(error)
  }
}
</script>

<style scoped>
* {
  box-sizing: border-box;
}

.app {
  width: 100vw;
  min-height: 100vh;
  margin-left: calc(50% - 50vw);
  background: #eaf3fc;
  color: #153b70;
  font-family: Arial, sans-serif;
}

.topbar {
  height: 59px;
  display: flex;
  align-items: center;
  background: #fff;
  border: 1px solid #c4d1e0;
}

.menu-button,
.logout,
.profile {
  height: 100%;
  border: 0;
  background: transparent;
  color: #153b70;
}

.menu-button {
  width: 25%;
  padding: 0 14px;
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 17px;
  font-weight: 700;
  text-align: left;
}

.brand {
  flex: 1;
  text-align: center;
  font-size: 17px;
  font-weight: 700;
  white-space: nowrap;
}

.brand strong {
  color: #1681d2;
}

.profile {
  width: 29%;
  padding: 7px 18px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  border-left: 1px solid #c4d1e0;
  border-right: 1px solid #c4d1e0;
  font-size: 9px;
  text-align: left;
}

.logout {
  width: 50px;
  font-size: 27px;
}

.content {
  padding: 20px 11px;
}

h1 {
  margin: 0 0 6px;
  font-size: 17px;
}

.intro {
  margin: 0 0 16px;
  font-size: 10px;
}

.search-bar {
  display: flex;
  flex-direction: column;
  gap: 6px;
  max-width: 420px;
  margin-bottom: 14px;
  font-size: 12px;
  font-weight: 700;
}

.search-bar input {
  width: 100%;
  min-height: 38px;
  padding: 0 12px;
  border: 1px solid #bdcde0;
  border-radius: 8px;
  background: white;
  color: #153b70;
  font: inherit;
  font-weight: 400;
}

.search-bar input:focus {
  outline: 2px solid #1681d2;
  outline-offset: 2px;
}

.empty-state {
  margin: 0;
  padding: 18px 12px;
  color: #526b89;
  font-size: 12px;
}

.user-list {
  overflow: hidden;
  background: #fff;
  border: 1px solid #bdcde0;
  border-radius: 8px;
}

.visually-hidden {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}

.list-heading,
.user-row {
  display: grid;
  grid-template-columns: 1.1fr 1.7fr 1fr 1fr;
  align-items: center;
  column-gap: 16px;
  padding: 0 4px;
}

.list-heading {
  min-height: 36px;
  border-bottom: 1px solid #153b70;
  font-size: 10px;
  font-weight: 700;
}

.user-row {
  min-height: 64px;
  border-bottom: 1px solid #9eb2cc;
}

.user-row:last-child {
  border-bottom: 0;
}

.user-info {
  display: flex;
  flex-direction: column;
  gap: 8px;
  font-size: 10px;
}

select {
  width: 100%;
  height: 27px;
  padding: 0 8px;
  border: 1px solid #bdcde0;
  border-radius: 8px;
  background: white;
  color: #153b70;
  font-size: 10px;
}

.button {
  min-height: 31px;
  padding: 0 8px;
  border: 0;
  border-radius: 8px;
  color: white;
  font-size: 10px;
  font-weight: 700;
  white-space: nowrap;
  cursor: pointer;
}

.button-primary {
  background: #153b70;
}

.button-danger {
  background: #810b0b;
}

.message {
  margin: 12px 0;
  padding: 10px;
  border-radius: 6px;
}

.message-empty {
  margin: 0;
  padding: 0;
}

.success {
  color: #166534;
  background: #dcfce7;
}

.error {
  color: #991b1b;
  background: #fee2e2;
}

@media (max-width: 600px) {
  .menu-button {
    width: auto;
  }

  .brand {
    font-size: 14px;
  }

  .profile {
    display: none;
  }

  .list-heading {
    display: none;
  }

  .user-row {
    grid-template-columns: minmax(0, 1fr);
    gap: 10px;
    padding: 12px;
  }

  .user-info {
    overflow-wrap: anywhere;
    font-size: 14px;
  }

  select,
  .button {
    min-height: 40px;
    font-size: 14px;
  }
}
</style>
