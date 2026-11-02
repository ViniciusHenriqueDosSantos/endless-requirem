using Godot;

// Qualquer coisa que leva dano do tiro do jogador além dos fantasmas
// (o chefe, os alvos do chefe...). O tiro chama TakeHit e some.
public interface IHittable
{
	void TakeHit(int damage, Vector2 direction);
}
