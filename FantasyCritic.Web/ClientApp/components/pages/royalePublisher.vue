<template>
  <div class="col-md-10 offset-md-1 col-sm-12">
    <div v-if="publisher">
      <div class="publisher-name">
        <h1>{{publisher.publisherName}}</h1>
      </div>
      <div class="row">
        <div class="col-md-12 col-lg-6">
          <h4>Player Name: {{publisher.playerName}}</h4>
          <h4>
            Year/Quarter:
            <router-link :to="{ name: 'criticsRoyale', params: {year: publisher.yearQuarter.year, quarter: publisher.yearQuarter.quarter }}">
              {{publisher.yearQuarter.year}}-Q{{publisher.yearQuarter.quarter}}
            </router-link>
          </h4>
          <h2>
            Total Points: {{publisher.totalFantasyPoints}}
          </h2>
        </div>

        <div class="col-md-12 col-lg-6">
          <h4>Remaining Budget: {{publisher.budget | money}}</h4>
          <div v-if="userIsPublisher">
            <b-button variant="primary" v-b-modal="'royalePurchaseGameForm'">Purchase a Game</b-button>
            <royalePurchaseGameForm :yearQuarter="publisher.yearQuarter" :userRoyalePublisher="publisher" v-on:gamePurchased="gamePurchased"></royalePurchaseGameForm>
          </div>
        </div>
      </div>

      <hr />
      <div class="alert alert-danger" v-if="errorInfo">{{errorInfo}}</div>

      <h1>Games</h1>
      <b-table striped bordered small responsive :items="publisher.publisherGames" :fields="allFields" v-if="publisher.publisherGames.length !== 0">
        <template slot="masterGame" slot-scope="data">
          <masterGamePopover :masterGame="data.item.masterGame"> </masterGamePopover>
        </template>
        <template slot="releaseDate" slot-scope="data">
          {{getReleaseDate(data.item.masterGame)}}
        </template>
        <template slot="amountSpent" slot-scope="data">
          {{ data.item.amountSpent | money }}
        </template>
        <template slot="advertisingMoney" slot-scope="data">
          {{ data.item.advertisingMoney | money }}
          <b-button variant="info" size="sm" v-if="userIsPublisher && !data.item.locked" v-on:click="setGameToSetBudget(data.item)">Set Budget</b-button>
        </template>
        <template slot="criticScore" slot-scope="data">
          {{ data.item.masterGame.criticScore | score(2) }}
        </template>
        <template slot="fantasyPoints" slot-scope="data">
          {{ data.item.fantasyPoints | score(2) }}
        </template>
        <template slot="timestamp" slot-scope="data">
          {{ data.item.timestamp | date }}
        </template>
        <template slot="sellGame" slot-scope="data">
          <b-button block variant="danger" v-if="!data.item.locked" v-on:click="setGameToSell(data.item)">Sell</b-button>
        </template>
      </b-table>
      <div v-else class="alert alert-info">
        <template v-if="userIsPublisher">
          You have not bought any games yet!
        </template>
        <template v-else>
          This publisher has not bought any games yet.
        </template>
      </div>
    </div>

    <b-modal id="sellRoyaleGameModal" ref="sellRoyaleGameModalRef" title="Sell Game" @ok="sellGame">
      <div v-if="gameToModify">
        <p>Are you sure you want to sell <strong>{{gameToModify.masterGame.gameName}}</strong>?</p>
        <p>You will get back half the money you bought it for, and any advertising money currently assigned to it.</p>
        <p>Money to recieve: <strong>{{gameToModify.amountSpent / 2 + gameToModify.advertisingMoney | money}}</strong></p>
      </div>
    </b-modal>

    <b-modal id="setAdvertisingMoneyModal" ref="setAdvertisingMoneyModalRef" title="Set Advertising Budget" @ok="setBudget">
      <div v-if="gameToModify">
        <p>How much money do you want to allocate to <strong>{{gameToModify.masterGame.gameName}}</strong>?</p>
        <p>Each dollar allocated will increase your fantasy points received by 5%</p>
        <p>You can spend up to $10 for a bonus of 50%.</p>
        <p>You can adjust this up until the game (or reviews) come out.</p>
        <div class="form-group row">
          <label for="advertisingBudgetToSet" class="col-sm-2 col-form-label">Budget</label>
          <div class="col-sm-10">
            <input class="form-control" v-model="advertisingBudgetToSet">
          </div>
        </div>
      </div>
    </b-modal>
  </div>
</template>

<script>
  import Vue from "vue";
  import axios from "axios";
  import moment from "moment";

  import MasterGamePopover from "components/modules/masterGamePopover";
  import RoyalePurchaseGameForm from "components/modules/modals/royalePurchaseGameForm";

  export default {
    props: ['publisherid'],
    data() {
      return {
        errorInfo: "",
        publisher: null,
        gameToModify: null,
        advertisingBudgetToSet: 0,
        gameFields: [
          { key: 'masterGame', label: 'Game', thClass:'bg-primary', stickyColumn: true },
          { key: 'releaseDate', label: 'Release Date', sortable: true, thClass: 'bg-primary' },
          { key: 'amountSpent', label: 'Amount Spent', thClass: 'bg-primary' },
          { key: 'advertisingMoney', label: 'Advertising Budget', thClass: 'bg-primary' },
          { key: 'criticScore', label: 'Critic Score', thClass: 'bg-primary' },
          { key: 'fantasyPoints', label: 'Fantasy Points', thClass: 'bg-primary' },
          { key: 'timestamp', label: 'Purchase Date', thClass: 'bg-primary' }
        ],
        userPublisherFields: [
          { key: 'sellGame', label: '', thClass: 'bg-primary', label: 'Sell' }
        ]
      }
    },
    components: {
      RoyalePurchaseGameForm,
      MasterGamePopover
    },
    computed: {
      isAuth() {
          return this.$store.getters.tokenIsCurrent();
      },
      userIsPublisher() {
        return this.isAuth && (this.publisher.userID === this.$store.getters.userInfo.userID);
      },
      allFields() {
          let conditionalFields = [];
          if (this.userIsPublisher) {
              conditionalFields = conditionalFields.concat(this.userPublisherFields);
          }
          return this.gameFields.concat(conditionalFields);
      }
    },
    methods: {
      fetchPublisher() {
        axios
          .get('/api/Royale/GetRoyalePublisher/' + this.publisherid)
          .then(response => {
            this.publisher = response.data;
          })
          .catch(returnedError => (this.error = returnedError));
      },
      gamePurchased(purchaseInfo) {
        this.fetchPublisher();
        let message = purchaseInfo.gameName + " was purchased for " + this.$options.filters.money(purchaseInfo.purchaseCost);
        let toast = this.$toasted.show(message, {
          theme: "primary",
          position: "top-right",
          duration: 5000
        });
      },
      setGameToSell(publisherGame) {
        this.gameToModify = publisherGame;
        this.$refs.sellRoyaleGameModalRef.show();
      },
      sellGame() {
        var request = {
          publisherID: this.publisher.publisherID,
          masterGameID: this.gameToModify.masterGame.masterGameID
        };

        axios
          .post('/api/royale/SellGame', request)
          .then(response => {
              this.fetchPublisher();
              let message = this.gameToModify.masterGame.gameName + " was sold for " + this.$options.filters.money(this.gameToModify.amountSpent / 2);
              let toast = this.$toasted.show(message, {
                theme: "primary",
                position: "top-right",
                duration: 5000
              });
            })
          .catch(response => {
            this.errorInfo = "You can't sell that game. " + response.response.data;
          });
      },
      setGameToSetBudget(publisherGame) {
        this.gameToModify = publisherGame;
        this.advertisingBudgetToSet = publisherGame.advertisingMoney;
        this.$refs.setAdvertisingMoneyModalRef.show();
      },
      setBudget() {
        var request = {
          publisherID: this.publisher.publisherID,
          masterGameID: this.gameToModify.masterGame.masterGameID,
          advertisingMoney: this.advertisingBudgetToSet
        };

        axios
          .post('/api/royale/SetAdvertisingMoney', request)
          .then(response => {
            this.fetchPublisher();
            this.advertisingBudgetToSet = 0;
            })
          .catch(response => {
            this.advertisingBudgetToSet = 0;
            this.errorInfo = "You can't set that budget. " + response.response.data;
          });
      },
      getReleaseDate(game) {
        if (game.releaseDate) {
          return moment(game.releaseDate).format('YYYY-MM-DD');
        }
        return game.estimatedReleaseDate + ' (Estimated)'
      },
      openCriticLink(game) {
        return "https://opencritic.com/game/" + game.openCriticID + "/a";
      }
    },
    mounted() {
        this.fetchPublisher();
    },
    watch: {
      '$route'(to, from) {
          this.fetchPublisher();
      }
    }
  }
</script>
<style scoped>
  .publisher-name {
    display: block;
    max-width: 100%;
    word-wrap: break-word;
  }
  .top-area{
    margin-top: 10px;
    margin-bottom: 20px;
  }

  .main-buttons {
    display: flex;
    flex-direction: row;
    justify-content: space-around;
  }

  .main-button{
    margin-top: 5px;
    min-width: 200px;
  }
</style>
