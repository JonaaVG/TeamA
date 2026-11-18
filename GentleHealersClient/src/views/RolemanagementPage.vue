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

    <main class="content">
      <h1>Rollen en rechten</h1>
      <p class="intro">Wijzig de rol van een bestaand gebruikersaccount.</p>

      <p v-if="successMessage" class="message success">{{ successMessage }}</p>
      <p v-if="errorMessage" class="message error">{{ errorMessage }}</p>

      <section class="user-list">
        <div class="list-heading">
          <span>Gebruiker</span>
          <span>Huidige rol</span>
          <span></span>
          <span></span>
        </div>

        <div v-for="user in users" :key="user.email" class="user-row">
          <div class="user-info">
            <span>{{ user.firstName }} {{ user.lastName }}</span>
            <span>{{ user.email }}</span>
          </div>

          <select v-model="user.roleId" :aria-label="`Rol voor ${user.firstName}`">
          <option v-for="role in roles" :key="role.roleId" :value="role.roleId">
            {{ role.name }}
          </option>
        </select>

        <button
          class="button button-primary"
          @click="changeRole(user)"
        >
          Rol wijzigen
        </button>
          <button class="button button-danger" @click="revokeAccess(user)">
            Rechten intrekken
          </button>
        </div>
      </section>
    </main>
  </div>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import api from '@/services/api'

const roles = ref([])
const users = ref([])
const errorMessage = ref('')
const successMessage = ref('')

onMounted(async () => {
  try {
    const [rolesResponse, usersResponse] = await Promise.all([
      api.get('/admin/roles/roles'),
      api.get('/admin/roles/users'),
    ])

    roles.value = rolesResponse.data
    users.value = usersResponse.data
  } catch (error) {
    errorMessage.value = 'De gebruikers of rollen konden niet worden geladen.'
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
      error.response?.data || 'De rol kon niet worden gewijzigd.'
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

.user-list {
  overflow: hidden;
  background: #fff;
  border: 1px solid #bdcde0;
  border-radius: 8px;
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

  .list-heading,
  .user-row {
    grid-template-columns: 1fr 1.2fr;
    gap: 8px;
    padding: 8px;
  }

  .list-heading span:nth-child(n + 3) {
    display: none;
  }
}
</style>
