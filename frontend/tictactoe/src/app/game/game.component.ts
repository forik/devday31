import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { DomSanitizer } from '@angular/platform-browser';

import { Game, GameService } from '../shared';

@Component({
  selector: 'app-game',
  templateUrl: './game.component.html',
  styleUrls: ['./game.component.css']
})
export class GameComponent implements OnInit {
  summary: Game;
  board: {};

  constructor(
    private http: HttpClient,
    private route: ActivatedRoute,
    private sanitizer: DomSanitizer,
    private service: GameService
  ) {}

  ngOnInit() {
    const gameId = this.route.snapshot.params['gameId'];
    this.board = {};
    this.http
      .get<any>(`game/getmoves/${gameId}`)
      .subscribe(data => this.renderBoard(data));
    this.service.onMoveMade.subscribe(data => this.renderBoard(data));
  }

  makeMove(x: number, y: number) {
    this.board[this.toCoords(x, y)] = this.symbol;
    this.summary.yourMove = false;
    this.service.makeMove(this.summary.gameId, x, y);
  }

  private renderBoard(data: any): void {
    this.summary = data.summary;
    if (data.summary.yourMove) {
      for (let x = 0; x < 3; x++) {
        for (let y = 0; y < 3; y++) {
          this.board[this.toCoords(x, y)] = 'MOVE';
        }
      }
    }
    let useO = true;
    data.moves.forEach(move => {
      this.board[this.toCoords(move.x, move.y)] = useO ? 'O' : 'X';
      useO = !useO;
    });
  }

  private toCoords(x: number, y: number): string {
    return `x${x}y${y}`;
  }

  get symbol(): string {
    return this.summary && this.summary.gameStarter ? 'O' : 'X';
  }
}
