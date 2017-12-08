import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

import { Game, GameMove } from '../shared';

@Component({
  selector: 'app-games',
  templateUrl: './games.component.html',
  styleUrls: ['./games.component.css']
})
export class GamesComponent implements OnInit {
  currentGames: Game[];
  availableGames: Game[];

  constructor(private http: HttpClient, private router: Router) {}

  ngOnInit() {
    this.load();
  }

  createGame(): void {
    this.http.post('game/creategame', null).subscribe(_ => this.load());
  }

  joinGame(): void {
    const gameId = prompt('Enter game id');
    this.http.post(`game/join/${gameId}`, null).subscribe(_ => this.load());
  }

  showInvite(gameId: string) {
    alert(gameId);
  }

  private load() {
    this.http.get('/game/index').subscribe(data => {
      this.currentGames = data[0];
      this.currentGames.forEach(x => (x.waiting = x.state === 0));
      this.availableGames = data[1];
    });
  }
}
